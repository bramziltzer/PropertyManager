using PropertyManager.Domain;

namespace PropertyManager.AppService.Properties;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertySummaryDto>> GetPropertiesAsync(CancellationToken cancellationToken = default);
    Task<PropertyDetailDto> GetPropertyAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SavePropertyRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, SavePropertyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitDto>> GetUnitsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<UnitDto> GetUnitAsync(int unitId, CancellationToken cancellationToken = default);
    Task<int> CreateUnitAsync(int propertyId, SaveUnitRequest request, CancellationToken cancellationToken = default);
    Task UpdateUnitAsync(int propertyId, int unitId, SaveUnitRequest request, CancellationToken cancellationToken = default);
    Task DeleteUnitAsync(int unitId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnitTypeOptionDto>> GetUnitTypeOptionsAsync(CancellationToken cancellationToken = default);
}
