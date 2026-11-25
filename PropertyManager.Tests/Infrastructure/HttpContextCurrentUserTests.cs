using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PropertyManager.Domain;
using PropertyManager.Infrastructure;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Tests.Infrastructure;

public class HttpContextCurrentUserTests
{
    [Fact]
    public void ApplicationServices_RegistersRequestInfrastructure()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICurrentUser>().Should().BeOfType<HttpContextCurrentUser>();
        provider.GetRequiredService<IHttpContextAccessor>().Should().NotBeNull();
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void Properties_ReadAuthenticatedUserClaims()
    {
        var userId = Guid.NewGuid();
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, RoleNames.PropertyManager)
                ], "Test"))
            }
        };
        var currentUser = new HttpContextCurrentUser(accessor);

        currentUser.Id.Should().Be(userId);
        currentUser.IsPropertyManager.Should().BeTrue();
    }

    [Fact]
    public void Id_ThrowsWhenRequestIsNotAuthenticated()
    {
        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });

        var act = () => currentUser.Id;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*authenticated requests*");
    }
}
