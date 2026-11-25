using Bogus;

namespace PropertyManager.Data.Seeds;

internal static class SeedData
{
    public const string ManagerEmail = "manager@propertymanager.test";
    public const string Password = "PropertyManager1!";

    public static readonly string[] ApplicantEmails =
    [
        "ada@propertymanager.test",
        "grace@propertymanager.test",
        "linus@propertymanager.test",
        "katherine@propertymanager.test",
        "margaret@propertymanager.test",
        "alan@propertymanager.test",
    ];

    public static Faker CreateFaker(int sequence)
    {
        Randomizer.Seed = new Random(8675309 + sequence);
        return new Faker();
    }
}
