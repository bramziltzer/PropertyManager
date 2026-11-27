using PropertyManager.AppService.Properties;

namespace PropertyManager.Models.PropertyModels;

public sealed record UnitListItemViewModel(
    int RentalUnitId,
    int PropertyId,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    string UnitTypeName,
    bool IsUnitTypeActive,
    bool HasApplicationsOrLeases,
    bool IsLeased = false)
{
    public static UnitListItemViewModel FromDto(UnitDto dto) => new(
        dto.RentalUnitId,
        dto.PropertyId,
        dto.UnitNumber,
        dto.Bedrooms,
        dto.MonthlyRent,
        dto.UnitTypeName,
        dto.IsUnitTypeActive,
        dto.HasApplicationsOrLeases,
        dto.IsLeased);
}

public sealed record PropertyUnitsViewModel(
    int PropertyId,
    IReadOnlyList<UnitListItemViewModel> Units);
