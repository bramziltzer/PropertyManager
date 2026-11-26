using PropertyManager.AppService.Applications;
using PropertyManager.Domain;

namespace PropertyManager.Models.ApplicationModels;

public sealed record ApplicationStatusChangeViewModel(
    Guid ApplicationStatusChangeId,
    ApplicationStatus OldStatus,
    ApplicationStatus NewStatus,
    string? Comment,
    string ChangedByDisplayName,
    DateTimeOffset ChangedDate)
{
    public string Outcome => NewStatus switch
    {
        ApplicationStatus.Submitted => "Submit",
        ApplicationStatus.Returned => "Return",
        ApplicationStatus.Approved => "Approve",
        ApplicationStatus.Denied => "Deny",
        ApplicationStatus.Withdrawn => "Withdraw",
        _ => NewStatus.ToString()
    };

    public static ApplicationStatusChangeViewModel FromDto(ApplicationStatusChangeDto dto) => new(
        dto.ApplicationStatusChangeId,
        dto.OldStatus,
        dto.NewStatus,
        dto.Comment,
        dto.ChangedByDisplayName,
        dto.ChangedDate);
}
