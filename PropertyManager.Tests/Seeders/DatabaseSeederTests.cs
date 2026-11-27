using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Data;
using PropertyManager.Data.Seeds;
using PropertyManager.Domain;

namespace PropertyManager.Tests.Seeders;

public class DatabaseSeederTests
{
    [Fact]
    public async Task Seeders_AreIdempotentAndCreateAllRequiredFixtures()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.RegisterSeeders();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        await SeedAll(scope.ServiceProvider);
        var firstCounts = await Counts(dbContext);

        await SeedAll(scope.ServiceProvider);
        var secondCounts = await Counts(dbContext);

        secondCounts.Should().BeEquivalentTo(firstCounts);
        firstCounts.Users.Should().Be(8);
        firstCounts.Properties.Should().Be(3);
        firstCounts.Units.Should().Be(9);
        firstCounts.Applications.Should().Be(6);
        firstCounts.Statuses.Should().Be(6);
        firstCounts.ActiveLeases.Should().Be(1);

        (await dbContext.Users.AnyAsync(user => user.Email == "manager2@example.com")).Should().BeTrue();
        (await dbContext.Users.AllAsync(user => user.Email == null || !user.Email.Contains("propertymanager")))
            .Should().BeTrue();

        var inactiveTypeWithUnit = await dbContext.UnitTypes.AnyAsync(type =>
            type.Kind == UnitTypeKind.Townhouse &&
            !type.IsActive &&
            dbContext.RentalUnits.Any(unit => unit.Kind == type.Kind));
        inactiveTypeWithUnit.Should().BeTrue();
    }

    private static async Task SeedAll(IServiceProvider services)
    {
        foreach (var seeder in services.GetServices<ISeeder>())
        {
            await seeder.Seed();
        }
    }

    private static async Task<SeedCounts> Counts(ApplicationDbContext dbContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var statuses = await dbContext.RentalApplications
            .Select(application => application.Status)
            .Distinct()
            .CountAsync();
        var activeLeases = await dbContext.RentalLeases
            .CountAsync(lease => lease.BeginDate <= today && lease.EndDate >= today);

        return new SeedCounts(
            await dbContext.Users.CountAsync(),
            await dbContext.Properties.CountAsync(),
            await dbContext.RentalUnits.CountAsync(),
            await dbContext.RentalApplications.CountAsync(),
            statuses,
            activeLeases);
    }

    private sealed record SeedCounts(
        int Users,
        int Properties,
        int Units,
        int Applications,
        int Statuses,
        int ActiveLeases);
}
