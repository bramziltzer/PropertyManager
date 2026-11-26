using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Tests.AppService;

public class RentalApplicationServiceTests
{
    [Fact]
    public async Task StartAsync_CreatesDraftRecordingCurrentApplicant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var applicationId = await service.StartAsync(unit.RentalUnitId);

        var saved = await dbContext.RentalApplications
            .AsNoTracking()
            .SingleAsync(a => a.RentalApplicationId == applicationId);
        saved.ApplicantUserId.Should().Be(TestData.ApplicantId);
        saved.RentalUnitId.Should().Be(unit.RentalUnitId);
        saved.Status.Should().Be(ApplicationStatus.Draft);
    }

    [Fact]
    public async Task StartAsync_RejectsNonApplicant_WithForbiddenException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ManagerId, isApplicant: false);

        await service.Invoking(s => s.StartAsync(unit.RentalUnitId))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task StartAsync_RejectsDuplicateOpenApplication()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.StartAsync(unit.RentalUnitId);

        await service.Invoking(s => s.StartAsync(unit.RentalUnitId))
            .Should().ThrowAsync<DuplicateOpenApplicationException>();
    }

    [Fact]
    public async Task StartAsync_AllowsNewApplication_WhenPreviousWasWithdrawn()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var previous = TestData.Draft(unit, TestData.ApplicantId);
        previous.Withdraw(TestData.ApplicantId, TestData.Now.AddDays(-1));
        dbContext.RentalApplications.Add(previous);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var newApplicationId = await service.StartAsync(unit.RentalUnitId);
        newApplicationId.Should().NotBe(previous.RentalApplicationId);
    }

    [Fact]
    public async Task StartAsync_RejectsUnitWithActiveLease()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser { Id = otherApplicantId, UserName = "other@example.com", Email = "other@example.com" });

        var approved = TestData.Submitted(unit, otherApplicantId);
        approved.Approve(TestData.ManagerId, TestData.Now.AddDays(-1), TestData.Today.AddDays(-1));
        dbContext.RentalApplications.Add(approved);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(s => s.StartAsync(unit.RentalUnitId))
            .Should().ThrowAsync<UnitNotAvailableException>();
    }

    private static async Task<RentalUnit> SeedUnitAsync(ApplicationDbContext dbContext)
    {
        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);
        dbContext.ApplicationStatusTypes.AddRange(
            new ApplicationStatusType { Status = ApplicationStatus.Draft, Name = "Draft" },
            new ApplicationStatusType { Status = ApplicationStatus.Submitted, Name = "Submitted" },
            new ApplicationStatusType { Status = ApplicationStatus.Returned, Name = "Returned" },
            new ApplicationStatusType { Status = ApplicationStatus.Approved, Name = "Approved" },
            new ApplicationStatusType { Status = ApplicationStatus.Denied, Name = "Denied" },
            new ApplicationStatusType { Status = ApplicationStatus.Withdrawn, Name = "Withdrawn" });
        dbContext.Users.AddRange(
            new ApplicationUser
            {
                Id = TestData.ApplicantId,
                UserName = "applicant@example.com",
                Email = "applicant@example.com"
            },
            new ApplicationUser
            {
                Id = TestData.ManagerId,
                UserName = "manager@example.com",
                Email = "manager@example.com"
            });

        var property = new Property("Oakridge", TestData.Address());
        var unit = new RentalUnit("101", 2, 1650m, apartment);
        property.AddUnit(unit);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        return unit;
    }

    private static RentalApplicationService CreateService(
        ApplicationDbContext dbContext,
        Guid userId,
        bool isApplicant) =>
        new(
            dbContext,
            new StubCurrentUser(userId, isApplicant),
            new FixedTimeProvider(TestData.Now));

    private static async Task<ApplicationDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private sealed class StubCurrentUser(Guid id, bool isApplicant) : ICurrentUser
    {
        public Guid Id => id;
        public bool IsApplicant => isApplicant;
        public bool IsPropertyManager => !isApplicant;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
