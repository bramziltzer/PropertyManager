using Microsoft.EntityFrameworkCore;
using PropertyManager.Domain;

namespace PropertyManager.Data.Seeds;

public class ApplicationStatusTypeSeeder(ApplicationDbContext dbContext) : ISeeder
{
    public async Task Seed(CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.ApplicationStatusTypes.ToDictionaryAsync(type => type.Status, cancellationToken);

        foreach (var status in Enum.GetValues<ApplicationStatus>())
        {
            if (existing.TryGetValue(status, out var statusType))
            {
                statusType.Name = status.ToString();
            }
            else
            {
                dbContext.ApplicationStatusTypes.Add(new ApplicationStatusType { Status = status, Name = status.ToString() });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
