using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Rules;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.AppService.Units;

public sealed class RentalUnitService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRentalUnitService
{
    public async Task<AvailableUnitsResultDto> GetAvailableUnitsAsync(
        AvailableUnitsFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var query = dbContext.RentalUnits
            .AsNoTracking()
            .Where(LeaseRules.IsAvailableOn(today));

        if (filter.PropertyId.HasValue)
        {
            query = query.Where(unit => unit.PropertyId == filter.PropertyId.Value);
        }

        if (filter.Bedrooms.HasValue)
        {
            query = query.Where(unit => unit.Bedrooms == filter.Bedrooms.Value);
        }

        if (filter.MaxRent.HasValue)
        {
            query = query.Where(unit => unit.MonthlyRent <= filter.MaxRent.Value);
        }

        var units = await query
            .OrderBy(unit => unit.Property.Name)
            .ThenBy(unit => unit.UnitNumber)
            .Select(unit => new AvailableUnitDto(
                unit.RentalUnitId,
                unit.PropertyId,
                unit.Property.Name,
                new AddressDto(
                    unit.Property.Address.Line1,
                    unit.Property.Address.Line2,
                    unit.Property.Address.City,
                    unit.Property.Address.State,
                    unit.Property.Address.ZipCode),
                unit.UnitNumber,
                unit.Bedrooms,
                unit.MonthlyRent,
                unit.UnitType.Name))
            .ToListAsync(cancellationToken);

        var propertyFilterOptions = await dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new PropertyFilterOptionDto(property.PropertyId, property.Name))
            .ToListAsync(cancellationToken);

        return new AvailableUnitsResultDto(units, propertyFilterOptions);
    }

    public async Task<UnitDetailsDto> GetUnitDetailsAsync(
        int unitId,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var details = await dbContext.RentalUnits
            .AsNoTracking()
            .Where(unit => unit.RentalUnitId == unitId)
            .Select(unit => new
            {
                unit.RentalUnitId,
                unit.PropertyId,
                PropertyName = unit.Property.Name,
                PropertyAddress = new AddressDto(
                    unit.Property.Address.Line1,
                    unit.Property.Address.Line2,
                    unit.Property.Address.City,
                    unit.Property.Address.State,
                    unit.Property.Address.ZipCode),
                unit.UnitNumber,
                unit.Bedrooms,
                unit.MonthlyRent,
                UnitTypeName = unit.UnitType.Name,
                IsAvailable = !unit.Leases.Any(lease => lease.BeginDate <= today && lease.EndDate >= today)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Unit {unitId} was not found.");

        Guid? existingOpenApplicationId = null;
        if (currentUser.IsApplicant)
        {
            var applicantId = currentUser.Id;
            existingOpenApplicationId = await dbContext.RentalApplications
                .AsNoTracking()
                .Where(app => app.ApplicantUserId == applicantId
                    && app.RentalUnitId == unitId
                    && ApplicationStatusRules.OpenStatuses.Contains(app.Status))
                .Select(app => (Guid?)app.RentalApplicationId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new UnitDetailsDto(
            details.RentalUnitId,
            details.PropertyId,
            details.PropertyName,
            details.PropertyAddress,
            details.UnitNumber,
            details.Bedrooms,
            details.MonthlyRent,
            details.UnitTypeName,
            details.IsAvailable,
            existingOpenApplicationId);
    }
}
