using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

public class UnitTypeSeeder(ApplicationDbContext dbContext) : ISeeder
{
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
                dbContext.UnitTypes.Add(new UnitType { Kind = kind, Name = kind.ToString() });
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
