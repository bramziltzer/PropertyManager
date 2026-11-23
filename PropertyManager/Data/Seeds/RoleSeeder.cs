using Microsoft.AspNetCore.Identity;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

/// <summary>
/// Seeds the application roles into the identity role store.
/// </summary>
/// <param name="roleManager">The role manager used to inspect and create application roles.</param>
public class RoleSeeder(RoleManager<IdentityRole> roleManager) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        foreach (AppRole role in AppRoleUtils.GetAllRoles())
        {
            if (!await roleManager.RoleExistsAsync(nameof(role)))
            {
                await roleManager.CreateAsync(new IdentityRole(nameof(role)));
            }
        }
    }
}