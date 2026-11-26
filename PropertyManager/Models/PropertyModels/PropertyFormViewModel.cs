using System.ComponentModel.DataAnnotations;
using PropertyManager.AppService.Properties;

namespace PropertyManager.Models;

public sealed class PropertyFormViewModel
{
    public int? PropertyId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Address Line 1")]
    public string Line1 { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Address Line 2")]
    public string? Line2 { get; set; }

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Enter a valid 5-digit or ZIP+4 code (e.g., 12345 or 12345-6789).")]
    [Display(Name = "ZIP Code")]
    public string ZipCode { get; set; } = string.Empty;

    public bool IsEdit => PropertyId.HasValue;
    public string Title => IsEdit ? "Edit Property" : "Add Property";
    public string SubmitLabel => IsEdit ? "Save Changes" : "Create Property";

    public SavePropertyRequest ToRequest() => new(
        Name,
        new AddressDto(Line1, Line2, City, State, ZipCode));

    public static PropertyFormViewModel FromDto(PropertyDetailDto dto) => new()
    {
        PropertyId = dto.PropertyId,
        Name = dto.Name,
        Line1 = dto.Address.Line1,
        Line2 = dto.Address.Line2,
        City = dto.Address.City,
        State = dto.Address.State,
        ZipCode = dto.Address.ZipCode
    };
}
