using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.AppService.Properties;

public sealed class PropertyService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IMemoryCache memoryCache,
    TimeProvider? timeProvider = null) : IPropertyService
{
    private const string UnitTypeOptionsCacheKey = "PropertyService.UnitTypeOptions";
    private static readonly TimeSpan UnitTypeOptionsCacheDuration = TimeSpan.FromMinutes(5);
    public async Task<IReadOnlyList<PropertySummaryDto>> GetPropertiesAsync(CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        var today = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);

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
                property.Units.Count,
                property.Units
                    .OrderBy(unit => unit.UnitNumber)
                    .Select(unit => new UnitDto(
                        unit.RentalUnitId,
                        unit.PropertyId,
                        unit.UnitNumber,
                        unit.Bedrooms,
                        unit.MonthlyRent,
                        unit.Kind,
                        unit.UnitType.Name,
                        unit.UnitType.IsActive,
                        unit.Applications.Any() || unit.Leases.Any(),
                        unit.Leases.Any(lease => lease.BeginDate <= today && lease.EndDate >= today)))
                    .ToList()))
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

    public async Task<IReadOnlyList<UnitDto>> GetUnitsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        var today = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);

        var propertyExists = await dbContext.Properties
            .AsNoTracking()
            .AnyAsync(item => item.PropertyId == propertyId, cancellationToken);

        if (!propertyExists)
        {
            throw new NotFoundException($"Property {propertyId} was not found.");
        }

        return await dbContext.RentalUnits
            .AsNoTracking()
            .Where(unit => unit.PropertyId == propertyId)
            .OrderBy(unit => unit.UnitNumber)
            .Select(unit => new UnitDto(
                unit.RentalUnitId,
                unit.PropertyId,
                unit.UnitNumber,
                unit.Bedrooms,
                unit.MonthlyRent,
                unit.Kind,
                unit.UnitType.Name,
                unit.UnitType.IsActive,
                unit.Applications.Any() || unit.Leases.Any(),
                unit.Leases.Any(lease => lease.BeginDate <= today && lease.EndDate >= today)))
            .ToListAsync(cancellationToken);
    }

    public async Task<UnitDto> GetUnitAsync(int unitId, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        var today = DateOnly.FromDateTime((timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime);

        var unit = await dbContext.RentalUnits
            .AsNoTracking()
            .Where(item => item.RentalUnitId == unitId)
            .Select(item => new UnitDto(
                item.RentalUnitId,
                item.PropertyId,
                item.UnitNumber,
                item.Bedrooms,
                item.MonthlyRent,
                item.Kind,
                item.UnitType.Name,
                item.UnitType.IsActive,
                item.Applications.Any() || item.Leases.Any(),
                item.Leases.Any(lease => lease.BeginDate <= today && lease.EndDate >= today)))
            .FirstOrDefaultAsync(cancellationToken);

        return unit ?? throw new NotFoundException($"Unit {unitId} was not found.");
    }

    public async Task<int> CreateUnitAsync(int propertyId, SaveUnitRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        ArgumentNullException.ThrowIfNull(request);

        var property = await dbContext.Properties
            .Include(item => item.Units)
            .FirstOrDefaultAsync(item => item.PropertyId == propertyId, cancellationToken)
            ?? throw new NotFoundException($"Property {propertyId} was not found.");

        var unitType = await dbContext.UnitTypes
            .FirstOrDefaultAsync(type => type.Kind == request.Kind, cancellationToken)
            ?? throw new NotFoundException($"Unit type {request.Kind} was not found.");

        // constructor enforces active unit type restriction
        var unit = new RentalUnit(request.UnitNumber, request.Bedrooms, request.MonthlyRent, unitType);
        property.AddUnit(unit);
        await dbContext.SaveChangesAsync(cancellationToken);

        return unit.RentalUnitId;
    }

    public async Task UpdateUnitAsync(int propertyId, int unitId, SaveUnitRequest request, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();
        ArgumentNullException.ThrowIfNull(request);

        var property = await dbContext.Properties
            .Include(item => item.Units)
            .FirstOrDefaultAsync(item => item.PropertyId == propertyId, cancellationToken)
            ?? throw new NotFoundException($"Property {propertyId} was not found.");

        var unit = property.Units.FirstOrDefault(item => item.RentalUnitId == unitId)
            ?? throw new NotFoundException($"Unit {unitId} was not found.");

        var unitType = await dbContext.UnitTypes
            .FirstOrDefaultAsync(type => type.Kind == request.Kind, cancellationToken)
            ?? throw new NotFoundException($"Unit type {request.Kind} was not found.");

        property.UpdateUnit(unit, request.UnitNumber, request.Bedrooms, request.MonthlyRent, unitType);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUnitAsync(int unitId, CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        var unit = await dbContext.RentalUnits
            .Include(item => item.Property)
            .Include(item => item.Leases)
            .Include(item => item.Applications)
            .FirstOrDefaultAsync(item => item.RentalUnitId == unitId, cancellationToken)
            ?? throw new NotFoundException($"Unit {unitId} was not found.");

        unit.Property.RemoveUnit(unit);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitTypeOptionDto>> GetUnitTypeOptionsAsync(CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        return (await memoryCache.GetOrCreateAsync(UnitTypeOptionsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = UnitTypeOptionsCacheDuration;

            return await dbContext.UnitTypes
                .AsNoTracking()
                .OrderBy(type => type.Name)
                .Select(type => new UnitTypeOptionDto(type.Kind, type.Name, type.IsActive))
                .ToListAsync(cancellationToken);
        }))!;
    }

    private void EnsurePropertyManager()
    {
        if (!currentUser.IsPropertyManager)
        {
            throw new ForbiddenException("Only property managers can manage properties.");
        }
    }
}
