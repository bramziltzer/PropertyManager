using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

public class RentalApplicationSeeder(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        var faker = SeedData.CreateFaker(3);
        var manager = await RequiredUser(SeedData.ManagerEmail);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var seeds = new[]
        {
            new ApplicationSeed(SeedData.ApplicantEmails[0], "Oakridge Apartments", "101", ApplicationStatus.Approved),
            new ApplicationSeed(SeedData.ApplicantEmails[1], "Oakridge Apartments", "102", ApplicationStatus.Submitted),
            new ApplicationSeed(SeedData.ApplicantEmails[2], "Harbor View Residences", "201", ApplicationStatus.Returned),
            new ApplicationSeed(SeedData.ApplicantEmails[3], "Harbor View Residences", "202", ApplicationStatus.Denied),
            new ApplicationSeed(SeedData.ApplicantEmails[4], "Maple Grove Townhomes", "301", ApplicationStatus.Draft),
            new ApplicationSeed(SeedData.ApplicantEmails[5], "Maple Grove Townhomes", "302", ApplicationStatus.Withdrawn),
        };

        for (var index = 0; index < seeds.Length; index++)
        {
            var seed = seeds[index];
            var applicant = await RequiredUser(seed.ApplicantEmail);
            var unit = await dbContext.RentalUnits
                .Include(existing => existing.Leases)
                .SingleAsync(
                    existing => existing.Property.Name == seed.PropertyName &&
                                existing.UnitNumber == seed.UnitNumber,
                    cancellationToken);

            var exists = await dbContext.RentalApplications.AnyAsync(
                application => application.ApplicantUserId == applicant.Id &&
                               application.RentalUnitId == unit.RentalUnitId,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            var application = RentalApplication.StartDraft(unit, applicant.Id, today);
            var submittedAt = now.AddHours(-2).AddMinutes(index);
            var name = faker.Name;
            application.SaveApplicantInfo(new ApplicantInfo(
                name.FirstName(),
                name.LastName(),
                faker.Phone.PhoneNumber("###-###-####"),
                applicant.Email!,
                FakeAddress()));
            application.AddResidence(
                FakeAddress(),
                faker.Name.FullName(),
                faker.Phone.PhoneNumber("###-###-####"),
                today.AddYears(-3),
                today.AddMonths(-1));

            if (seed.Status != ApplicationStatus.Draft)
            {
                application.Submit(applicant.Id, submittedAt, today);
            }

            switch (seed.Status)
            {
                case ApplicationStatus.Approved:
                    application.Approve(manager.Id, submittedAt.AddMinutes(1), today, "Application approved.");
                    break;
                case ApplicationStatus.Returned:
                    application.Return(manager.Id, submittedAt.AddMinutes(1), "Please verify the residence dates.");
                    break;
                case ApplicationStatus.Denied:
                    application.Deny(manager.Id, submittedAt.AddMinutes(1), "The application did not meet our criteria.");
                    break;
                case ApplicationStatus.Withdrawn:
                    application.Withdraw(applicant.Id, submittedAt.AddMinutes(1));
                    break;
            }

            dbContext.RentalApplications.Add(application);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        async Task<ApplicationUser> RequiredUser(string email) =>
            await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Seeded user '{email}' was not created.");

        Address FakeAddress() => new(
            faker.Address.StreetAddress(),
            null,
            faker.Address.City(),
            faker.Address.StateAbbr(),
            faker.Address.ZipCode("#####"));
    }

    private sealed record ApplicationSeed(
        string ApplicantEmail,
        string PropertyName,
        string UnitNumber,
        ApplicationStatus Status);
}
