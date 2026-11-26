using System.Security.Claims;
using PropertyManager.Domain;

namespace PropertyManager.Infrastructure.CurrentUser;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid Id
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new InvalidOperationException("A current user is only available for authenticated requests.");
            }

            var identifier = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(identifier, out var id))
            {
                throw new InvalidOperationException("The authenticated user's identifier is missing or invalid.");
            }

            return id;
        }
    }

    public bool IsApplicant =>
        httpContextAccessor.HttpContext?.User.IsInRole(RoleNames.Applicant) == true;

    public bool IsPropertyManager =>
        httpContextAccessor.HttpContext?.User.IsInRole(RoleNames.PropertyManager) == true;
}
