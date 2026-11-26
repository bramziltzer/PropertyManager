using System.ComponentModel.DataAnnotations;
using PropertyManager.AppService.Reviews;
using PropertyManager.Data.Configurations;

namespace PropertyManager.Models.ApplicationModels;

public sealed class ReviewFormViewModel : IValidatableObject
{
    public Guid RentalApplicationId { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    public string UnitNumber { get; set; } = string.Empty;

    public string ApplicantDisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a review outcome.")]
    [Display(Name = "Outcome")]
    public ReviewOutcome? Outcome { get; set; }

    [StringLength(ColumnLengths.Comment)]
    [Display(Name = "Comment")]
    public string? Comment { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Outcome is ReviewOutcome.Return or ReviewOutcome.Deny &&
            string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult(
                "A comment is required when returning or denying an application.",
                [nameof(Comment)]);
        }
    }

    public static ReviewFormViewModel FromDto(ReviewDetailsDto dto) => new()
    {
        RentalApplicationId = dto.RentalApplicationId,
        PropertyName = dto.PropertyName,
        UnitNumber = dto.UnitNumber,
        ApplicantDisplayName = dto.ApplicantDisplayName
    };

    public ReviewFormViewModel WithDetails(ReviewDetailsDto dto)
    {
        RentalApplicationId = dto.RentalApplicationId;
        PropertyName = dto.PropertyName;
        UnitNumber = dto.UnitNumber;
        ApplicantDisplayName = dto.ApplicantDisplayName;
        return this;
    }
}
