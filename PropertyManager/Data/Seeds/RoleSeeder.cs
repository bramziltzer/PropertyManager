using Microsoft.AspNetCore.Identity;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

/// <summary>
/// Seeds the application roles into the identity role store.
/// </summary>
/// <param name="roleManager">The role manager used to inspect and create application roles.</param>
public class RoleSeeder(RoleManager<IdentityRole<Guid>> roleManager) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        foreach (AppRole role in AppRoleUtils.GetAllRoles())
        {
            var roleName = AppRoleUtils.GetRoleName(role);
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }
}