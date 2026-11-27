using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Exceptions;
using PropertyManager.AppService.Units;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Tests.AppService;

public class RentalUnitServiceTests
{
    [Fact]
    public async Task GetAvailableUnitsAsync_ExcludesActiveLeaseAndIncludesFutureOrExpiredLeases()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);
        dbContext.ApplicationStatusTypes.AddRange(
            new ApplicationStatusType { Status = ApplicationStatus.Draft, Name = "Draft" },
            new ApplicationStatusType { Status = ApplicationStatus.Submitted, Name = "Submitted" },
            new ApplicationStatusType { Status = ApplicationStatus.Approved, Name = "Approved" });
        dbContext.Users.AddRange(
            new ApplicationUser { Id = TestData.ApplicantId, UserName = "applicant@example.com", Email = "applicant@example.com" },
            new ApplicationUser { Id = TestData.ManagerId, UserName = "manager@example.com", Email = "manager@example.com" });

        var property = new Property("Oakridge", TestData.Address());
        var activeLeaseUnit = new RentalUnit("101", 2, 1600m, apartment);
        var futureLeaseUnit = new RentalUnit("102", 2, 1600m, apartment);
        var expiredLeaseUnit = new RentalUnit("103", 2, 1600m, apartment);
        var noLeaseUnit = new RentalUnit("104", 2, 1600m, apartment);
        property.AddUnit(activeLeaseUnit);
        property.AddUnit(futureLeaseUnit);
        property.AddUnit(expiredLeaseUnit);
        property.AddUnit(noLeaseUnit);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        // Active lease covering TestData.Today (2026-06-01)
        var activeApp = RentalApplication.StartDraft(activeLeaseUnit, TestData.ApplicantId, TestData.Today.AddDays(-10));
        activeApp.SaveApplicantInfo(TestData.Info());
        activeApp.AddResidence(TestData.Address(), "Landlord", "555-0101", new DateOnly(2020, 1, 1), null, TestData.Today);
        activeApp.Submit(TestData.ApplicantId, TestData.Now.AddDays(-10), TestData.Today.AddDays(-10));
        activeApp.Approve(TestData.ManagerId, TestData.Now.AddDays(-10), TestData.Today.AddDays(-10));
        dbContext.RentalApplications.Add(activeApp);

        // Future non-current lease starting after TestData.Today
        var futureApp = RentalApplication.StartDraft(futureLeaseUnit, TestData.ApplicantId, TestData.Today);
        futureApp.SaveApplicantInfo(TestData.Info());
        futureApp.AddResidence(TestData.Address(), "Landlord", "555-0101", new DateOnly(2020, 1, 1), null, TestData.Today);
        futureApp.Submit(TestData.ApplicantId, TestData.Now, TestData.Today);
        var futureStart = TestData.Today.AddMonths(1);
        futureApp.Approve(TestData.ManagerId, TestData.Now, futureStart);
        dbContext.RentalApplications.Add(futureApp);

        // Expired lease ending before TestData.Today
        var expiredApp = RentalApplication.StartDraft(expiredLeaseUnit, TestData.ApplicantId, TestData.Today.AddYears(-2));
        expiredApp.SaveApplicantInfo(TestData.Info());
        expiredApp.AddResidence(TestData.Address(), "Landlord", "555-0101", new DateOnly(2020, 1, 1), null, TestData.Today);
        expiredApp.Submit(TestData.ApplicantId, TestData.Now.AddYears(-2), TestData.Today.AddYears(-2));
        expiredApp.Approve(TestData.ManagerId, TestData.Now.AddYears(-2), TestData.Today.AddYears(-2));
        dbContext.RentalApplications.Add(expiredApp);

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.GetAvailableUnitsAsync(new AvailableUnitsFilter());

        result.Units.Select(u => u.UnitNumber).Should().BeEquivalentTo(["102", "103", "104"]);
    }

    [Fact]
    public async Task GetAvailableUnitsAsync_AppliesPropertyBedroomsAndMaxRentFilters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);

        var oakridge = new Property("Oakridge", TestData.Address());
        oakridge.AddUnit(new RentalUnit("101", 1, 1200m, apartment));
        oakridge.AddUnit(new RentalUnit("102", 2, 1600m, apartment));
        oakridge.AddUnit(new RentalUnit("103", 2, 2200m, apartment));

        var harbor = new Property("Harbor View", TestData.Address());
        harbor.AddUnit(new RentalUnit("201", 2, 1500m, apartment));

        dbContext.Properties.AddRange(oakridge, harbor);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var filtered = await service.GetAvailableUnitsAsync(
            new AvailableUnitsFilter(PropertyId: oakridge.PropertyId, Bedrooms: 2, MaxRent: 1800m));

        filtered.Units.Should().ContainSingle(u => u.UnitNumber == "102");
        filtered.PropertyFilterOptions.Select(p => p.Name).Should().ContainInOrder("Harbor View", "Oakridge");
    }

    [Fact]
    public async Task GetUnitDetailsAsync_ReturnsDetailsAndThrowsNotFound_WhenMissing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);
        dbContext.ApplicationStatusTypes.Add(new ApplicationStatusType { Status = ApplicationStatus.Draft, Name = "Draft" });
        dbContext.Users.Add(new ApplicationUser { Id = TestData.ApplicantId, UserName = "applicant@example.com", Email = "applicant@example.com" });

        var property = new Property("Oakridge", TestData.Address());
        var unit = new RentalUnit("101", 2, 1650m, apartment);
        property.AddUnit(unit);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        var draft = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(draft);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var details = await service.GetUnitDetailsAsync(unit.RentalUnitId);
        details.UnitNumber.Should().Be("101");
        details.IsAvailable.Should().BeTrue();
        details.ExistingOpenApplicationId.Should().Be(draft.RentalApplicationId);

        await service.Invoking(s => s.GetUnitDetailsAsync(9999))
            .Should().ThrowAsync<NotFoundException>();
    }

    private static RentalUnitService CreateService(ApplicationDbContext dbContext) =>
        new(
            dbContext,
            new StubCurrentUser(TestData.ApplicantId, isApplicant: true),
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
