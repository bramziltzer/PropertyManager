using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Domain;

public class RentalApplication
{
    public RentalApplication(RentalUnit unit, ApplicationStatus status)
    {
        Unit = unit;
        Status = status;
    }

    public RentalUnit Unit { get; set; }

    // For now, only one applicant is allowed per application, but this could be changed in the future.
    public ApplicantInfo ApplicantInfo { get; set; }
    
    public List<ResidenceHistory> ResidenceHistories { get; set; } = new List<ResidenceHistory>();

    public ApplicationStatus Status { get; private set; }

    public string? PropertyManagerComments { get; private set; }
    
    public List<ApplicationStatusChange> History { get; set; }
    // State Machine:
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
    /// Updates the application status and property manager comments.
    /// </summary>
    /// <param name="newStatus">The new application status.</param>
    /// <param name="propertyManagerComments">Comments from the property manager.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newStatus"/> is <see cref="ApplicationStatus.Returned"/> or <see
    /// cref="ApplicationStatus.Denied"/> and <paramref name="propertyManagerComments"/> is null, empty, or whitespace.</exception>
    public void ApplyAction(ApplicationAction action, string appliedBy, DateTime now, string? comments = null)
    {
        if (!Transitions.TryGetValue((this.Status, action), out ApplicationStatus nextStatus)) 
        {
            throw new InvalidStatusTransitionException(this.Status, action);
        }

        if (ApplicationStatusRules.RequiresComment(nextStatus) && string.IsNullOrWhiteSpace(comments))
        {
            // TODO change this to custom exception
            throw new ArgumentException("Property manager comments are required for the new application status {0}.", nameof(nextStatus));
        }

        History.Add(new ApplicationStatusChange(Status, nextStatus, comments, appliedBy, now));
        Status = nextStatus;
        PropertyManagerComments = comments; // todo this may be best kept in the history table

        // replace status and comments properties with a status collection with date and user
    }
}
