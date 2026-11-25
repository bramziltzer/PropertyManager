using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Domain;

public class RentalApplication
{
    public RentalApplication()
    {
    }

    public RentalApplication(RentalUnit unit, ApplicationStatus status)
    {
        Unit = unit;
        RentalUnitId = unit.RentalUnitId;
        Status = status;
    }

    public Guid RentalApplicationId { get; set; }

    public int RentalUnitId { get; set; }
    public RentalUnit Unit { get; set; } = null!;

    public ApplicantInfo? ApplicantInfo { get; set; }

    public ICollection<ResidenceHistory> ResidenceHistories { get; set; } = [];
    public RentalLease? Lease { get; set; }
    public ApplicationStatus Status { get; private set; }

    public string? PropertyManagerComments { get; private set; }

    public ICollection<ApplicationStatusChange> History { get; set; } = [];

    private static readonly Dictionary<(ApplicationStatus, ApplicationAction), ApplicationStatus> Transitions = new()
    {
        [(ApplicationStatus.Draft, ApplicationAction.Submit)] = ApplicationStatus.Submitted,
        [(ApplicationStatus.Draft, ApplicationAction.Withdraw)] = ApplicationStatus.Withdrawn,
        [(ApplicationStatus.Returned, ApplicationAction.Submit)] = ApplicationStatus.Submitted,
        [(ApplicationStatus.Returned, ApplicationAction.Withdraw)] = ApplicationStatus.Withdrawn,
        [(ApplicationStatus.Submitted, ApplicationAction.Withdraw)] = ApplicationStatus.Withdrawn,
        [(ApplicationStatus.Submitted, ApplicationAction.Return)] = ApplicationStatus.Returned,
        [(ApplicationStatus.Submitted, ApplicationAction.Approve)] = ApplicationStatus.Approved,
        [(ApplicationStatus.Submitted, ApplicationAction.Deny)] = ApplicationStatus.Denied,
    };

    /// <summary>
    /// Applies an allowed action and records the resulting status change.
    /// </summary>
    /// <param name="action">The action to apply.</param>
    /// <param name="appliedBy">The id of the user applying the action.</param>
    /// <param name="now">The time the action was applied.</param>
    /// <param name="comments">Optional comments from the property manager.</param>
    /// <exception cref="InvalidStatusTransitionException">Thrown when the action is not allowed for the current status.</exception>
    /// <exception cref="ReviewCommentRequiredException">Thrown when a Return or Deny action has no comment.</exception>
    public void ApplyAction(ApplicationAction action, Guid appliedBy, DateTimeOffset now, string? comments = null)
    {
        if (!Transitions.TryGetValue((Status, action), out ApplicationStatus nextStatus))
        {
            throw new InvalidStatusTransitionException(Status, action);
        }

        if (ApplicationStatusRules.RequiresComment(nextStatus) && string.IsNullOrWhiteSpace(comments))
        {
            throw new ReviewCommentRequiredException(nextStatus);
        }

        // TODO consider reducing duplications stored in the db
        History.Add(new ApplicationStatusChange(this.RentalApplicationId, Status, nextStatus, comments, appliedBy, now));
        Status = nextStatus;
        PropertyManagerComments = comments;
    }
}
