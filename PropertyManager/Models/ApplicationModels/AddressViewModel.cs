using System.ComponentModel.DataAnnotations;
using PropertyManager.AppService;

namespace PropertyManager.Models.ApplicationModels;

public sealed class AddressViewModel
{
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

    public AddressDto ToDto() => new(Line1, Line2, City, State, ZipCode);

    public string FormatSingleLine()
    {
        var street = string.IsNullOrWhiteSpace(Line2)
            ? Line1
            : $"{Line1}, {Line2}";

        return $"{street}, {City}, {State} {ZipCode}";
    }

    public static AddressViewModel FromDto(AddressDto dto) => new()
    {
        Line1 = dto.Line1,
        Line2 = dto.Line2,
        City = dto.City,
        State = dto.State,
        ZipCode = dto.ZipCode
    };
}
