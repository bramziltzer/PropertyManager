using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyManager.AppService.Applications;
using PropertyManager.Domain;

namespace PropertyManager.Models.ApplicationModels;

public sealed class ApplicationListViewModel
{
    public ApplicationListFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<ApplicationRowViewModel> Applications { get; set; } = [];

    public bool IsPropertyManager { get; set; }

    public static ApplicationListViewModel FromResult(
        ApplicationListResultDto result,
        ApplicationStatus? status,
        int? propertyId)
    {
        return new ApplicationListViewModel
        {
            Filter = new ApplicationListFilterViewModel
            {
                Status = status,
                PropertyId = propertyId,
                StatusOptions = Enum.GetValues<ApplicationStatus>()
                    .Select(value => new SelectListItem(
                        value.ToString(),
                        value.ToString(),
                        value == status))
                    .ToList(),
                PropertyOptions = result.PropertyFilterOptions
                    .Select(option => new SelectListItem(
                        option.Name,
                        option.PropertyId.ToString(),
                        option.PropertyId == propertyId))
                    .ToList()
            },
            Applications = result.Applications
                .Select(ApplicationRowViewModel.FromDto)
                .ToList(),
            IsPropertyManager = result.IsPropertyManager
        };
    }
}

public sealed class ApplicationListFilterViewModel
{
    [Display(Name = "Status")]
    public ApplicationStatus? Status { get; set; }

    [Display(Name = "Property")]
    public int? PropertyId { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    public IReadOnlyList<SelectListItem> PropertyOptions { get; set; } = [];
}

public sealed record ApplicationRowViewModel(
    Guid RentalApplicationId,
    string PropertyName,
    string UnitNumber,
    decimal MonthlyRent,
    ApplicationStatus Status,
    string ApplicantDisplayName,
    bool IsEditable)
{
    public static ApplicationRowViewModel FromDto(ApplicationListItemDto dto) => new(
        dto.RentalApplicationId,
        dto.PropertyName,
        dto.UnitNumber,
        dto.MonthlyRent,
        dto.Status,
        dto.ApplicantDisplayName,
        dto.IsEditable);
}
