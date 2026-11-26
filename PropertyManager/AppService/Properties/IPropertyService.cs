namespace PropertyManager.AppService.Properties;

public interface IPropertyService
{
    Task<IReadOnlyList<PropertySummaryDto>> GetPropertiesAsync(CancellationToken cancellationToken = default);
    Task<PropertyDetailDto> GetPropertyAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(SavePropertyRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, SavePropertyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
