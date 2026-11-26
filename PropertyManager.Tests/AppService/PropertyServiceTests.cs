using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService;
using PropertyManager.AppService.Exceptions;
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

        var service = new PropertyService(dbContext, new StubCurrentUser(TestData.ManagerId, isPropertyManager: true));

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

        var service = new PropertyService(dbContext, new StubCurrentUser(TestData.ManagerId, isPropertyManager: true));
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

        var service = new PropertyService(dbContext, new StubCurrentUser(TestData.ManagerId, isPropertyManager: true));

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

        var service = new PropertyService(dbContext, new StubCurrentUser(TestData.ManagerId, isPropertyManager: true));

        await service.DeleteAsync(property.PropertyId);

        (await dbContext.Properties.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task NonManager_ThrowsForbiddenException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var service = new PropertyService(dbContext, new StubCurrentUser(TestData.ApplicantId, isPropertyManager: false));

        await service.Invoking(s => s.GetPropertiesAsync()).Should().ThrowAsync<ForbiddenException>();
    }

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
