namespace PropertyManager.AppService.Units;

public interface IRentalUnitService
{
    Task<AvailableUnitsResultDto> GetAvailableUnitsAsync(
        AvailableUnitsFilter filter,
        CancellationToken cancellationToken = default);

    Task<UnitDetailsDto> GetUnitDetailsAsync(
        int unitId,
        CancellationToken cancellationToken = default);
}
