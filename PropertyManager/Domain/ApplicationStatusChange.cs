namespace PropertyManager.Domain;

public class ApplicationStatusChange
{
    public Guid ApplicationStatusChangeId { get; set; }
    public Guid RentalApplicationId { get; set; }
    public ApplicationStatus OldStatus { get; set; }
    public ApplicationStatus NewStatus { get; set; }
    public string? Comment { get; set; }
    public Guid ChangedByUserId { get; set; }
    public DateTimeOffset ChangedDate { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;
    public ApplicationUser ChangeByUser { get; set; } = null!;

    public ApplicationStatusChange()
    {
    }

    public ApplicationStatusChange(
        Guid rentalApplicationId,
        ApplicationStatus oldStatus,
        ApplicationStatus newStatus,
        string? comment,
        Guid changedByUserId,
        DateTimeOffset changedDate)
    {
        RentalApplicationId = rentalApplicationId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Comment = comment;
        ChangedByUserId = changedByUserId;
        ChangedDate = changedDate;
    }
}