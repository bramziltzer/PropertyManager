using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Exceptions;
using PropertyManager.AppService.Reviews;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Tests.AppService;

public class ReviewServiceTests
{
    [Fact]
    public async Task Approve_CreatesOneTwelveMonthLeaseForApplicant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateReviewService(dbContext, TestData.ManagerId, isPropertyManager: true);

        await service.ReviewAsync(application.RentalApplicationId, ReviewOutcome.Approve, "Welcome!");

        var savedApp = await dbContext.RentalApplications
            .AsNoTracking()
            .Include(a => a.Lease)
            .SingleAsync(a => a.RentalApplicationId == application.RentalApplicationId);

        savedApp.Status.Should().Be(ApplicationStatus.Approved);
        savedApp.Lease.Should().NotBeNull();
        savedApp.Lease!.LesseeId.Should().Be(TestData.ApplicantId);
        savedApp.Lease.RentalUnitId.Should().Be(unit.RentalUnitId);
        savedApp.Lease.BeginDate.Should().Be(TestData.Today);
        savedApp.Lease.EndDate.Should().Be(TestData.Today.AddMonths(12).AddDays(-1));
    }

    [Fact]
    public async Task Approve_SecondApplicationForSameUnit_IsRejectedAndLeavesOtherSubmitted()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = otherApplicantId,
            UserName = "second@example.com",
            Email = "second@example.com"
        });

        var firstApp = TestData.Submitted(unit, TestData.ApplicantId);
        var secondApp = TestData.Submitted(unit, otherApplicantId);
        dbContext.RentalApplications.AddRange(firstApp, secondApp);
        await dbContext.SaveChangesAsync();

        var service = CreateReviewService(dbContext, TestData.ManagerId, isPropertyManager: true);

        await service.ReviewAsync(firstApp.RentalApplicationId, ReviewOutcome.Approve, null);

        await service.Invoking(s => s.ReviewAsync(secondApp.RentalApplicationId, ReviewOutcome.Approve, null))
            .Should().ThrowAsync<UnitNotAvailableException>();

        var reloadedSecond = await dbContext.RentalApplications
            .AsNoTracking()
            .SingleAsync(a => a.RentalApplicationId == secondApp.RentalApplicationId);

        reloadedSecond.Status.Should().Be(ApplicationStatus.Submitted);
        (await dbContext.RentalLeases.CountAsync(l => l.RentalUnitId == unit.RentalUnitId)).Should().Be(1);
    }

    [Theory]
    [InlineData(ReviewOutcome.Return)]
    [InlineData(ReviewOutcome.Deny)]
    public async Task ReturnAndDeny_RequireComment(ReviewOutcome outcome)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateReviewService(dbContext, TestData.ManagerId, isPropertyManager: true);

        await service.Invoking(s => s.ReviewAsync(application.RentalApplicationId, outcome, "   "))
            .Should().ThrowAsync<ReviewCommentRequiredException>();

        var reloaded = await dbContext.RentalApplications
            .AsNoTracking()
            .SingleAsync(a => a.RentalApplicationId == application.RentalApplicationId);
        reloaded.Status.Should().Be(ApplicationStatus.Submitted);
    }

    [Fact]
    public async Task ReturnedApplication_ApplicantCanCorrectAndResubmit_AndManagerCanApprove()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var reviewService = CreateReviewService(dbContext, TestData.ManagerId, isPropertyManager: true);
        var appService = CreateApplicationService(dbContext, TestData.ApplicantId, isApplicant: true);

        await reviewService.ReviewAsync(
            application.RentalApplicationId,
            ReviewOutcome.Return,
            "Please update your phone number.");

        await appService.SaveApplicantInfoAsync(
            application.RentalApplicationId,
            new SaveApplicantInfoDto(
                "Jane",
                "Doe",
                "555-9999",
                "jane@example.com",
                new PropertyManager.AppService.AddressDto("123 Main St", null, "Boston", "MA", "02101")));

        await appService.SubmitAsync(application.RentalApplicationId);
        await reviewService.ReviewAsync(application.RentalApplicationId, ReviewOutcome.Approve, "Looks good.");

        var reloaded = await dbContext.RentalApplications
            .AsNoTracking()
            .Include(a => a.Lease)
            .SingleAsync(a => a.RentalApplicationId == application.RentalApplicationId);

        reloaded.Status.Should().Be(ApplicationStatus.Approved);
        reloaded.ApplicantInfo!.PhoneNumber.Should().Be("555-9999");
        reloaded.Lease.Should().NotBeNull();
    }

    [Fact]
    public async Task Approve_ConcurrencyFailure_ThrowsFriendlyConcurrencyException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Guid applicationId;
        await using (var setupContext = await CreateDbContextAsync(connection))
        {
            var unit = await SeedUnitAsync(setupContext);
            var application = TestData.Submitted(unit, TestData.ApplicantId);
            setupContext.RentalApplications.Add(application);
            await setupContext.SaveChangesAsync();
            applicationId = application.RentalApplicationId;
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var throwingContext = new ConcurrencyThrowingDbContext(options);

        var service = CreateReviewService(throwingContext, TestData.ManagerId, isPropertyManager: true);

        var act = () => service.ReviewAsync(applicationId, ReviewOutcome.Approve, null);

        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("*reload and try again*");
    }

    [Fact]
    public async Task GetHistoryAsync_ProjectsReviewerEmailTimestampStatusesOutcomeAndComment()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateReviewService(dbContext, TestData.ManagerId, isPropertyManager: true);
        await service.ReviewAsync(application.RentalApplicationId, ReviewOutcome.Deny, "Insufficient income.");

        var history = await service.GetHistoryAsync(application.RentalApplicationId);

        history.Should().HaveCount(2);
        var denyEntry = history.Should().ContainSingle(e => e.NewStatus == ApplicationStatus.Denied).Subject;
        denyEntry.OldStatus.Should().Be(ApplicationStatus.Submitted);
        denyEntry.Comment.Should().Be("Insufficient income.");
        denyEntry.ChangedByDisplayName.Should().Be("manager@example.com");
        denyEntry.ChangedDate.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task ReviewMethods_RejectNonManager_WithForbiddenException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateReviewService(dbContext, TestData.ApplicantId, isPropertyManager: false);

        await service.Invoking(s => s.GetReviewAsync(application.RentalApplicationId))
            .Should().ThrowAsync<ForbiddenException>();
        await service.Invoking(s => s.ReviewAsync(application.RentalApplicationId, ReviewOutcome.Approve, null))
            .Should().ThrowAsync<ForbiddenException>();
        await service.Invoking(s => s.GetHistoryAsync(application.RentalApplicationId))
            .Should().ThrowAsync<ForbiddenException>();
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

    private static ReviewService CreateReviewService(
        ApplicationDbContext dbContext,
        Guid userId,
        bool isPropertyManager) =>
        new(
            dbContext,
            new StubCurrentUser(userId, !isPropertyManager, isPropertyManager),
            new FixedTimeProvider(TestData.Now));

    private static RentalApplicationService CreateApplicationService(
        ApplicationDbContext dbContext,
        Guid userId,
        bool isApplicant) =>
        new(
            dbContext,
            new StubCurrentUser(userId, isApplicant, !isApplicant),
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

    private sealed class ConcurrencyThrowingDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new DbUpdateConcurrencyException("Simulated concurrency conflict.");
    }

    private sealed class StubCurrentUser(Guid id, bool isApplicant, bool isPropertyManager) : ICurrentUser
    {
        public Guid Id => id;
        public bool IsApplicant => isApplicant;
        public bool IsPropertyManager => isPropertyManager;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
