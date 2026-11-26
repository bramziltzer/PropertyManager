using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;

namespace PropertyManager.AppService;

public static class PropertyFilterQueryExtensions
{
    /// <summary>
    /// Fetches the list of property filter options for dropdowns.
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<IReadOnlyList<PropertyFilterOptionDto>> GetPropertyFilterOptionsAsync(
        this ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new PropertyFilterOptionDto(property.PropertyId, property.Name))
            .ToListAsync(cancellationToken);
    }
}
