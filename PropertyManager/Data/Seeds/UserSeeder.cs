using Bogus;
using Microsoft.AspNetCore.Identity;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

public class UserSeeder(UserManager<ApplicationUser> userManager) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        var faker = SeedData.CreateFaker(1);
        await EnsureUser(SeedData.ManagerEmail, RoleNames.PropertyManager, faker, cancellationToken);
        await EnsureUser(SeedData.SecondManagerEmail, RoleNames.PropertyManager, faker, cancellationToken);

        foreach (var email in SeedData.ApplicantEmails)
        {
            await EnsureUser(email, RoleNames.Applicant, faker, cancellationToken);
        }
    }

    private async Task EnsureUser(
        string email,
        string role,
        Faker faker,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = faker.Phone.PhoneNumber("###-###-####"),
            };

            var createResult = await userManager.CreateAsync(user, SeedData.Password);
            ThrowIfFailed(createResult, $"create seeded user '{email}'");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            ThrowIfFailed(roleResult, $"assign role '{role}' to seeded user '{email}'");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {operation}: {errors}");
    }
}
