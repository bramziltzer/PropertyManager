namespace PropertyManager.Data.Seeds;

public static class SeedExtensions
{
    public static IServiceCollection RegisterSeeders(this IServiceCollection services)
    {
        // register database seeders here
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UnitTypeSeeder>();
        services.AddScoped<ISeeder, ApplicationStatusTypeSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, PropertyUnitSeeder>();
        services.AddScoped<ISeeder, RentalApplicationSeeder>();

        return services;
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        foreach (var seeder in services.GetServices<ISeeder>())
        {
            await seeder.Seed();
        }
    }
}
