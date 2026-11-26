using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PropertyManager.AppService;
using PropertyManager.AppService.Exceptions;
using PropertyManager.AppService.Properties;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Tests.AppService;

public class PropertyServiceTests
{
    [Fact]
    public async Task CreateAndUpdate_PersistValidValues()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var service = CreateService(dbContext);

        var propertyId = await service.CreateAsync(new SavePropertyRequest(
            " Oakwood Manor ",
            new AddressDto(" 100 Oak St ", " Suite 2 ", " Madison ", " WI ", " 53703 ")));

        var created = await service.GetPropertyAsync(propertyId);
        created.Name.Should().Be("Oakwood Manor");
        created.Address.Should().Be(new AddressDto("100 Oak St", "Suite 2", "Madison", "WI", "53703"));
        created.UnitCount.Should().Be(0);

        await service.UpdateAsync(propertyId, new SavePropertyRequest(
            "Oakwood Commons",
            new AddressDto("200 Pine Ave", "  ", "Milwaukee", "WI", "53202")));

        var updated = await service.GetPropertyAsync(propertyId);
        updated.Name.Should().Be("Oakwood Commons");
        updated.Address.Should().Be(new AddressDto("200 Pine Ave", null, "Milwaukee", "WI", "53202"));

        var list = await service.GetPropertiesAsync();
        list.Should().ContainSingle(item => item.PropertyId == propertyId && item.Name == "Oakwood Commons");
    }

    [Fact]
    public async Task MissingProperty_ThrowsNotFoundException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var service = CreateService(dbContext);
        var request = new SavePropertyRequest("Missing", new AddressDto("1 Main", null, "City", "ST", "12345"));

        await service.Invoking(s => s.GetPropertyAsync(999)).Should().ThrowAsync<NotFoundException>();
        await service.Invoking(s => s.UpdateAsync(999, request)).Should().ThrowAsync<NotFoundException>();
        await service.Invoking(s => s.DeleteAsync(999)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsRemovalBlockedException_WhenPropertyHasUnits()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unitType = TestData.ActiveType();
        dbContext.UnitTypes.Add(unitType);
        var property = new Property("With Units", TestData.Address());
        property.AddUnit(new RentalUnit("101", 2, 1500m, unitType));
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.Invoking(s => s.DeleteAsync(property.PropertyId))
            .Should().ThrowAsync<RemovalBlockedException>();

        (await dbContext.Properties.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProperty_WhenPropertyHasNoUnits()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var property = new Property("Empty Property", TestData.Address());
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.DeleteAsync(property.PropertyId);

        (await dbContext.Properties.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task NonManager_ThrowsForbiddenException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var service = CreateService(dbContext, isPropertyManager: false);

        await service.Invoking(s => s.GetPropertiesAsync()).Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetUnitTypeOptionsAsync_ReturnsAllTypesWithActiveStatus_AndCachesResult()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        dbContext.UnitTypes.AddRange(
            TestData.ActiveType(UnitTypeKind.Apartment),
            TestData.ActiveType(UnitTypeKind.House),
            TestData.InactiveType(UnitTypeKind.Townhouse));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var options = await service.GetUnitTypeOptionsAsync();
        options.Should().HaveCount(3);
        options.Where(o => o.IsActive).Select(o => o.Kind).Should().BeEquivalentTo([UnitTypeKind.Apartment, UnitTypeKind.House]);
        options.Single(o => !o.IsActive).Kind.Should().Be(UnitTypeKind.Townhouse);

        // Remove from DB and verify cached result is still returned
        dbContext.UnitTypes.RemoveRange(dbContext.UnitTypes);
        await dbContext.SaveChangesAsync();

        var cached = await service.GetUnitTypeOptionsAsync();
        cached.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAndUpdateUnit_RejectsInactiveTypeForNewOrChangedUnit_AndAllowsRetainingInactiveType()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        var townhouse = TestData.InactiveType(UnitTypeKind.Townhouse);
        dbContext.UnitTypes.AddRange(apartment, townhouse);

        var property = new Property("Maple Court", TestData.Address());
        var existingInactiveUnit = new RentalUnit("101", 2, 1400m, TestData.ActiveType(UnitTypeKind.Condo));
        property.AddUnit(existingInactiveUnit);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        // Mark Condo inactive directly in DB so existing unit retains an inactive type.
        var condoType = await dbContext.UnitTypes.SingleAsync(t => t.Kind == UnitTypeKind.Condo);
        condoType.IsActive = false;
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        // New unit cannot use inactive type
        await service.Invoking(s => s.CreateUnitAsync(
                property.PropertyId,
                new SaveUnitRequest("102", 2, 1600m, UnitTypeKind.Townhouse)))
            .Should().ThrowAsync<InactiveUnitTypeException>();

        // Existing unit cannot switch to a different inactive type
        await service.Invoking(s => s.UpdateUnitAsync(
                property.PropertyId,
                existingInactiveUnit.RentalUnitId,
                new SaveUnitRequest("101", 3, 1700m, UnitTypeKind.Townhouse)))
            .Should().ThrowAsync<InactiveUnitTypeException>();

        // Existing unit CAN retain its own inactive type while updating other fields
        await service.UpdateUnitAsync(
            property.PropertyId,
            existingInactiveUnit.RentalUnitId,
            new SaveUnitRequest("101A", 3, 1750m, UnitTypeKind.Condo));

        var updated = await service.GetUnitAsync(existingInactiveUnit.RentalUnitId);
        updated.UnitNumber.Should().Be("101A");
        updated.Bedrooms.Should().Be(3);
        updated.MonthlyRent.Should().Be(1750m);
        updated.Kind.Should().Be(UnitTypeKind.Condo);
        updated.IsUnitTypeActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndUpdateUnit_RejectsDuplicateUnitNumbersWithinProperty()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);

        var property = new Property("Oakridge", TestData.Address());
        property.AddUnit(new RentalUnit("101", 1, 1200m, apartment));
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.Invoking(s => s.CreateUnitAsync(
                property.PropertyId,
                new SaveUnitRequest("101", 2, 1500m, UnitTypeKind.Apartment)))
            .Should().ThrowAsync<DuplicateUnitNumberException>();

        var secondUnitId = await service.CreateUnitAsync(
            property.PropertyId,
            new SaveUnitRequest("102", 2, 1500m, UnitTypeKind.Apartment));

        await service.Invoking(s => s.UpdateUnitAsync(
                property.PropertyId,
                secondUnitId,
                new SaveUnitRequest("101", 2, 1500m, UnitTypeKind.Apartment)))
            .Should().ThrowAsync<DuplicateUnitNumberException>();
    }

    [Fact]
    public async Task DeleteUnitAsync_BlocksUnitsWithApplicationsOrLeases_AndRemovesUnusedUnit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);

        var property = new Property("Cedar", TestData.Address());
        var unitWithApp = new RentalUnit("101", 1, 1200m, apartment);
        var unusedUnit = new RentalUnit("102", 2, 1500m, apartment);
        property.AddUnit(unitWithApp);
        property.AddUnit(unusedUnit);
        dbContext.Properties.Add(property);

        var applicant = new ApplicationUser { Id = TestData.ApplicantId, UserName = "applicant@example.com", Email = "applicant@example.com" };
        dbContext.Users.Add(applicant);
        dbContext.ApplicationStatusTypes.Add(new ApplicationStatusType { Status = ApplicationStatus.Draft, Name = "Draft" });
        dbContext.RentalApplications.Add(TestData.Draft(unitWithApp, TestData.ApplicantId));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        await service.Invoking(s => s.DeleteUnitAsync(unitWithApp.RentalUnitId))
            .Should().ThrowAsync<RemovalBlockedException>();

        await service.DeleteUnitAsync(unusedUnit.RentalUnitId);

        var remaining = await service.GetUnitsAsync(property.PropertyId);
        remaining.Should().ContainSingle(u => u.RentalUnitId == unitWithApp.RentalUnitId);

        var properties = await service.GetPropertiesAsync();
        properties.Should().ContainSingle(p => p.PropertyId == property.PropertyId)
            .Which.Units.Should().ContainSingle(u => u.RentalUnitId == unitWithApp.RentalUnitId && u.HasApplicationsOrLeases);
    }

    private static PropertyService CreateService(ApplicationDbContext dbContext, bool isPropertyManager = true) =>
        new(
            dbContext,
            new StubCurrentUser(isPropertyManager ? TestData.ManagerId : TestData.ApplicantId, isPropertyManager),
            new MemoryCache(new MemoryCacheOptions()));

    private static async Task<ApplicationDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private sealed class StubCurrentUser(Guid id, bool isPropertyManager) : ICurrentUser
    {
        public Guid Id => id;
        public bool IsPropertyManager => isPropertyManager;
    }
}
