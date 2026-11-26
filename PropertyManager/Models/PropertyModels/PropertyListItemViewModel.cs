using PropertyManager.AppService.Properties;

namespace PropertyManager.Models.PropertyModels;

public sealed record PropertyListItemViewModel(
    int PropertyId,
    string Name,
    string FormattedAddress,
    int UnitCount,
    IReadOnlyList<UnitListItemViewModel> Units)
{
    public static PropertyListItemViewModel FromDto(PropertySummaryDto dto)
    {
        var street = string.IsNullOrWhiteSpace(dto.Address.Line2)
            ? dto.Address.Line1
            : $"{dto.Address.Line1}, {dto.Address.Line2}";

        return new PropertyListItemViewModel(
            dto.PropertyId,
            dto.Name,
            $"{street}, {dto.Address.City}, {dto.Address.State} {dto.Address.ZipCode}",
            dto.UnitCount,
            dto.Units.Select(UnitListItemViewModel.FromDto).ToList());
    }
}
