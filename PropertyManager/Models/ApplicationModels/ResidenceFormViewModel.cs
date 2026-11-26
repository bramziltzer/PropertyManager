using System.ComponentModel.DataAnnotations;
using PropertyManager.AppService;
using PropertyManager.AppService.Applications;
using PropertyManager.Data.Configurations;

namespace PropertyManager.Models.ApplicationModels;

public sealed class ResidenceFormViewModel : IValidatableObject
{
    public Guid RentalApplicationId { get; set; }

    public Guid? ResidenceHistoryId { get; set; }

    public AddressViewModel Address { get; set; } = new();

    [Required]
    [StringLength(200)]
    [Display(Name = "Landlord Name")]
    public string LandlordName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(ColumnLengths.PhoneNumber)]
    [Display(Name = "Landlord Phone")]
    public string LandlordPhone { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Move-in Date")]
    public DateOnly? MoveInDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Move-out Date")]
    public DateOnly? MoveOutDate { get; set; }

    public bool IsEdit => ResidenceHistoryId.HasValue;

    public string Title => IsEdit ? "Edit Residence" : "Add Residence";

    public string SubmitLabel => IsEdit ? "Save Changes" : "Add Residence";

    public SaveResidenceDto ToDto() => new(
        Address.ToDto(),
        LandlordName,
        LandlordPhone,
        MoveInDate!.Value,
        MoveOutDate);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MoveInDate.HasValue &&
            MoveOutDate.HasValue &&
            MoveOutDate.Value < MoveInDate.Value)
        {
            yield return new ValidationResult(
                "Move-out date cannot be before the move-in date.",
                [nameof(MoveOutDate)]);
        }
    }

    public static ResidenceFormViewModel FromDto(Guid applicationId, ResidenceDto dto) => new()
    {
        RentalApplicationId = applicationId,
        ResidenceHistoryId = dto.ResidenceHistoryId,
        Address = AddressViewModel.FromDto(dto.Address),
        LandlordName = dto.LandlordName,
        LandlordPhone = dto.LandlordPhone,
        MoveInDate = dto.MoveInDate,
        MoveOutDate = dto.MoveOutDate
    };
}
