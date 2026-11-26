using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManager.AppService;
using PropertyManager.AppService.Units;

namespace PropertyManager.Models.UnitModels;

public sealed class AvailableUnitsListViewModel
{
    [Display(Name = "Property")]
    public int? PropertyId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Bedrooms must be zero or greater.")]
    public int? Bedrooms { get; set; }

    [Display(Name = "Max Monthly Rent")]
    [Range(0, double.MaxValue, ErrorMessage = "Maximum rent must be zero or greater.")]
    public decimal? MaxRent { get; set; }

    public IReadOnlyList<SelectListItem> PropertyFilterOptions { get; set; } = [];

    public IReadOnlyList<AvailableUnitItemViewModel> Units { get; set; } = [];

    public static AvailableUnitsListViewModel FromResult(
        AvailableUnitsResultDto result,
        int? propertyId,
        int? bedrooms,
        decimal? maxRent)
    {
        return new AvailableUnitsListViewModel
        {
            PropertyId = propertyId,
            Bedrooms = bedrooms,
            MaxRent = maxRent,
            PropertyFilterOptions = result.PropertyFilterOptions
                .Select(option => new SelectListItem(
                    option.Name,
                    option.PropertyId.ToString(),
                    option.PropertyId == propertyId))
                .ToList(),
            Units = result.Units
                .Select(AvailableUnitItemViewModel.FromDto)
                .ToList()
        };
    }
}

public sealed record AvailableUnitItemViewModel(
    int RentalUnitId,
    string PropertyName,
    string FormattedAddress,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    string UnitTypeName)
{
    public static AvailableUnitItemViewModel FromDto(AvailableUnitDto dto) => new(
        dto.RentalUnitId,
        dto.PropertyName,
        FormatAddress(dto.PropertyAddress),
        dto.UnitNumber,
        dto.Bedrooms,
        dto.MonthlyRent,
        dto.UnitTypeName);

    internal static string FormatAddress(AddressDto address)
    {
        var street = string.IsNullOrWhiteSpace(address.Line2)
            ? address.Line1
            : $"{address.Line1}, {address.Line2}";

        return $"{street}, {address.City}, {address.State} {address.ZipCode}";
    }
}
