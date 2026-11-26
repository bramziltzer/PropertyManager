using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Properties;
using PropertyManager.AppService.Units;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Infrastructure;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ApplicationExceptionFilter>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IRentalUnitService, RentalUnitService>();
        services.AddScoped<IRentalApplicationService, RentalApplicationService>();

        return services;
    }
}
