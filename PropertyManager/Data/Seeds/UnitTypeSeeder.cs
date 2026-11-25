using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

public class UnitTypeSeeder(ApplicationDbContext dbContext) : ISeeder
{
    // Seeded as inactive for testing purposes
    private static readonly HashSet<UnitTypeKind> InactiveKinds = [UnitTypeKind.Townhouse];

    public async Task Seed(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UnitTypes.ToDictionaryAsync(unitType => unitType.Kind, cancellationToken);

        foreach (var kind in UnitTypeKindUtils.GetAllKinds())
        {
            if (existing.TryGetValue(kind, out var unitType))
            {
                unitType.Name = kind.ToString();
            }
            else
            {
                dbContext.UnitTypes.Add(new UnitType { Kind = kind, Name = kind.ToString(), IsActive = !InactiveKinds.Contains(kind) });
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
