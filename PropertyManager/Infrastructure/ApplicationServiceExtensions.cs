using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Infrastructure;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ApplicationExceptionFilter>();

        return services;
    }
}
