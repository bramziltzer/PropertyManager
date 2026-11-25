using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

public class PropertyUnitSeeder(ApplicationDbContext dbContext) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        var faker = SeedData.CreateFaker(2);
        var types = await dbContext.UnitTypes.ToDictionaryAsync(type => type.Kind, cancellationToken);
        var townhouseType = types[UnitTypeKind.Townhouse];
        var townhouseWasActive = townhouseType.IsActive;
        var townhouseWasTemporarilyActivated = false;

        var properties = new[]
        {
            new PropertySeed(
                "Oakridge Apartments",
                FakeAddress(),
                [
                    new UnitSeed("101", 2, 1650m, UnitTypeKind.Apartment),
                    new UnitSeed("102", 1, 1350m, UnitTypeKind.Apartment),
                    new UnitSeed("103", 3, 2100m, UnitTypeKind.Condo),
                ]),
            new PropertySeed(
                "Harbor View Residences",
                FakeAddress(),
                [
                    new UnitSeed("201", 2, 1925m, UnitTypeKind.Condo),
                    new UnitSeed("202", 3, 2375m, UnitTypeKind.Condo),
                    new UnitSeed("203", 4, 2850m, UnitTypeKind.House),
                ]),
            new PropertySeed(
                "Maple Grove Townhomes",
                FakeAddress(),
                [
                    new UnitSeed("301", 2, 1800m, UnitTypeKind.Townhouse),
                    new UnitSeed("302", 3, 2250m, UnitTypeKind.House),
                    new UnitSeed("303", 2, 1775m, UnitTypeKind.Apartment),
                ]),
        };

        foreach (var seed in properties)
        {
            var property = await dbContext.Properties
                .Include(existing => existing.Units)
                .SingleOrDefaultAsync(existing => existing.Name == seed.Name, cancellationToken);

            if (property is null)
            {
                property = new Property(seed.Name, seed.Address);
                dbContext.Properties.Add(property);
            }

            foreach (var unit in seed.Units)
            {
                if (property.Units.Any(existing => existing.UnitNumber == unit.Number))
                {
                    continue;
                }

                // The fixture models a unit that retained its type before the type was
                // deactivated; only activate the lookup while creating that unit.
                if (unit.Kind == UnitTypeKind.Townhouse && !townhouseType.IsActive)
                {
                    townhouseType.IsActive = true;
                    townhouseWasTemporarilyActivated = true;
                }

                property.AddUnit(new RentalUnit(
                    unit.Number,
                    unit.Bedrooms,
                    unit.MonthlyRent,
                    types[unit.Kind]));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (townhouseWasTemporarilyActivated)
        {
            townhouseType.IsActive = townhouseWasActive;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        Address FakeAddress() => new(
            faker.Address.StreetAddress(),
            null,
            faker.Address.City(),
            faker.Address.StateAbbr(),
            faker.Address.ZipCode("#####"));
    }

    private sealed record PropertySeed(string Name, Address Address, UnitSeed[] Units);

    private sealed record UnitSeed(
        string Number,
        int Bedrooms,
        decimal MonthlyRent,
        UnitTypeKind Kind);
}
