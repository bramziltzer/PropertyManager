using PropertyManager.AppService.Units;

namespace PropertyManager.Models.UnitModels;

public sealed record UnitDetailsViewModel(
    int RentalUnitId,
    int PropertyId,
    string PropertyName,
    string FormattedAddress,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    string UnitTypeName,
    bool IsAvailable,
    Guid? ExistingOpenApplicationId)
{
    public static UnitDetailsViewModel FromDto(UnitDetailsDto dto) => new(
        dto.RentalUnitId,
        dto.PropertyId,
        dto.PropertyName,
        AvailableUnitItemViewModel.FormatAddress(dto.PropertyAddress),
        dto.UnitNumber,
        dto.Bedrooms,
        dto.MonthlyRent,
        dto.UnitTypeName,
        dto.IsAvailable,
        dto.ExistingOpenApplicationId);
}
