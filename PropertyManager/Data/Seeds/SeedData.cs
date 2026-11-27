using Bogus;

namespace PropertyManager.Data.Seeds;

internal static class SeedData
{
    public const string ManagerEmail = "manager@example.com";
    public const string SecondManagerEmail = "manager2@example.com";
    public const string Password = "PropertyManager1!";

    public static readonly string[] ApplicantEmails =
    [
        "ada@example.com",
        "grace@example.com",
        "linus@example.com",
        "katherine@example.com",
        "margaret@example.com",
        "alan@example.com",
    ];

    public static Faker CreateFaker(int sequence)
    {
        Randomizer.Seed = new Random(8675309 + sequence);
        return new Faker();
    }
}
