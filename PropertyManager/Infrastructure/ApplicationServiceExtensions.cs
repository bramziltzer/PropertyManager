using PropertyManager.AppService.Properties;
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

        return services;
    }
}
