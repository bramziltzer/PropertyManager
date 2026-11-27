using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PropertyManager.AppService.Applications;
using PropertyManager.Data.Configurations;
using PropertyManager.Domain.Rules;

namespace PropertyManager.Models.ApplicationModels;

public sealed class ApplicantInfoSectionViewModel : IValidatableObject
{
    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [StringLength(ColumnLengths.PhoneNumber)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    public AddressViewModel CurrentAddress { get; set; } = new();

    [BindNever]
    public bool IsReadOnly { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!PhoneNumberRules.IsValid(PhoneNumber))
        {
            yield return new ValidationResult(
                "Enter a valid phone number.",
                [nameof(PhoneNumber)]);
        }
    }

    public ApplicantInfoSectionViewModel AsReadOnly() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        PhoneNumber = PhoneNumber,
        Email = Email,
        CurrentAddress = CurrentAddress,
        IsReadOnly = true
    };

    public SaveApplicantInfoDto ToDto() => new(
        FirstName,
        LastName,
        PhoneNumber,
        Email,
        CurrentAddress.ToDto());

    public static ApplicantInfoSectionViewModel FromDto(ApplicantInfoDto? dto, bool isReadOnly)
    {
        if (dto is null)
        {
            return new ApplicantInfoSectionViewModel
            {
                IsReadOnly = isReadOnly
            };
        }

        return new ApplicantInfoSectionViewModel
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            CurrentAddress = AddressViewModel.FromDto(dto.CurrentAddress),
            IsReadOnly = isReadOnly
        };
    }
}
