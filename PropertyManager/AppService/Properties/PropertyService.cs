using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.AppService.Properties;

public sealed class PropertyService(ApplicationDbContext dbContext, ICurrentUser currentUser) : IPropertyService
{
    public async Task<IReadOnlyList<PropertySummaryDto>> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        return await dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new PropertySummaryDto(
                property.PropertyId,
                property.Name,
                new AddressDto(
                    property.Address.Line1,
                    property.Address.Line2,
                    property.Address.City,
                    property.Address.State,
                    property.Address.ZipCode),
                property.Units.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyDetailDto> GetPropertyAsync(int id, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        var property = await dbContext.Properties
            .AsNoTracking()
            .Where(item => item.PropertyId == id)
            .Select(item => new PropertyDetailDto(
                item.PropertyId,
                item.Name,
                new AddressDto(
                    item.Address.Line1,
                    item.Address.Line2,
                    item.Address.City,
                    item.Address.State,
                    item.Address.ZipCode),
                item.Units.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return property ?? throw new NotFoundException($"Property {id} was not found.");
    }

    public async Task<int> CreateAsync(SavePropertyRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        ArgumentNullException.ThrowIfNull(request);

        var property = new Property(request.Name, request.Address.ToDomainAddress());
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        return property.PropertyId;
    }

    public async Task UpdateAsync(int id, SavePropertyRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        ArgumentNullException.ThrowIfNull(request);

        var property = await dbContext.Properties
            .FirstOrDefaultAsync(item => item.PropertyId == id, cancellationToken)
            ?? throw new NotFoundException($"Property {id} was not found.");

        property.Update(request.Name, request.Address.ToDomainAddress());
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        var property = await dbContext.Properties
            .Include(item => item.Units)
            .FirstOrDefaultAsync(item => item.PropertyId == id, cancellationToken)
            ?? throw new NotFoundException($"Property {id} was not found.");

        property.EnsureRemovable();
        dbContext.Properties.Remove(property);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsurePropertyManager()
    {
        if (!currentUser.IsPropertyManager)
        {
            throw new ForbiddenException("Only property managers can manage properties.");
        }
    }
}
