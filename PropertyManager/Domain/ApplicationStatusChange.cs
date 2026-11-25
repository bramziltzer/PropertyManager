using Microsoft.AspNetCore.Identity;

namespace PropertyManager.Domain;

public class ApplicationStatusChange
{
    public Guid ApplicationStatusChangeId { get; set; }
    public Guid RentalApplicationId { get; set; }
    public ApplicationStatus OldStatus { get; set; }
    public ApplicationStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;
    public IdentityUser Actor { get; set; } = null!;

    public ApplicationStatusChange()
    {
    }

    public ApplicationStatusChange(
        Guid rentalApplicationId,
        ApplicationStatus oldStatus,
        ApplicationStatus newStatus,
        string? comment,
        string changedByUserId,
        DateTime changedDate)
    {
        RentalApplicationId = rentalApplicationId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Comment = comment;
        ChangedByUserId = changedByUserId;
        ChangedDate = changedDate;
    }
}