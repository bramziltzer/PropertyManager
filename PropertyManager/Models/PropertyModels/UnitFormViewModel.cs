using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManager.AppService.Properties;
using PropertyManager.Domain;

namespace PropertyManager.Models.PropertyModels;

public sealed class UnitFormViewModel
{
    public int? RentalUnitId { get; set; }
    public int PropertyId { get; set; }

    [Required]
    [StringLength(50)]
    [Display(Name = "Unit Number")]
    public string UnitNumber { get; set; } = string.Empty;

    [Range(0, 20)]
    public int Bedrooms { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [DataType(DataType.Currency)]
    [Display(Name = "Monthly Rent")]
    public decimal MonthlyRent { get; set; }

    [Required]
    [Display(Name = "Unit Type")]
    public UnitTypeKind Kind { get; set; } = UnitTypeKind.Apartment;

    public IReadOnlyList<SelectListItem> UnitTypeOptions { get; private set; } = [];

    public bool IsEdit => RentalUnitId.HasValue;
    public string Title => IsEdit ? "Edit Unit" : "Add Unit";
    public string SubmitLabel => IsEdit ? "Save Changes" : "Create Unit";

    public SaveUnitRequest ToRequest() => new(UnitNumber, Bedrooms, MonthlyRent, Kind);

    public UnitFormViewModel WithOptions(IReadOnlyList<UnitTypeOptionDto> options, UnitTypeKind? currentKind = null)
    {
        UnitTypeOptions = options
            .Where(option => option.IsActive || (currentKind.HasValue && option.Kind == currentKind.Value))
            .Select(option => new SelectListItem(
                option.IsActive ? option.Name : $"{option.Name} (Inactive)",
                ((int)option.Kind).ToString(),
                option.Kind == Kind))
            .ToList();
        return this;
    }

    public static UnitFormViewModel FromDto(UnitDto dto, IReadOnlyList<UnitTypeOptionDto> options) =>
        new UnitFormViewModel
        {
            RentalUnitId = dto.RentalUnitId,
            PropertyId = dto.PropertyId,
            UnitNumber = dto.UnitNumber,
            Bedrooms = dto.Bedrooms,
            MonthlyRent = dto.MonthlyRent,
            Kind = dto.Kind
        }.WithOptions(options, dto.Kind);
}
