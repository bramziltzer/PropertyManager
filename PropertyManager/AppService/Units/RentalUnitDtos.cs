namespace PropertyManager.AppService.Units;

public sealed record AvailableUnitsFilter(
    int? PropertyId = null,
    int? Bedrooms = null,
    decimal? MaxRent = null);

public sealed record AvailableUnitDto(
    int RentalUnitId,
    int PropertyId,
    string PropertyName,
    AddressDto PropertyAddress,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    string UnitTypeName);

public sealed record AvailableUnitsResultDto(
    IReadOnlyList<AvailableUnitDto> Units,
    IReadOnlyList<PropertyFilterOptionDto> PropertyFilterOptions);

public sealed record UnitDetailsDto(
    int RentalUnitId,
    int PropertyId,
    string PropertyName,
    AddressDto PropertyAddress,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    string UnitTypeName,
    bool IsAvailable,
    Guid? ExistingOpenApplicationId);
