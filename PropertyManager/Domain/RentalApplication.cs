using PropertyManager.Domain.Exceptions;
using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class RentalApplication
{
    private const string ApplicantInfoSection = "Applicant Information";
    private const string ResidenceHistorySection = "Residence History";

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

    private readonly List<ResidenceHistory> _residenceHistories = [];
    private readonly List<ApplicationStatusChange> _history = [];

    private RentalApplication()
    {
    }

    /// <summary>
    /// Starts a draft application for the unit. Requires the unit's leases to be loaded.
    /// </summary>
    /// <exception cref="UnitNotAvailableException">Thrown when the unit has an active lease.</exception>
    public static RentalApplication StartDraft(RentalUnit unit, Guid applicantUserId, DateOnly today)
    {
        unit.EnsureAvailable(today);

        return new RentalApplication
        {
            Unit = unit,
            RentalUnitId = unit.RentalUnitId,
            ApplicantUserId = applicantUserId,
            Status = ApplicationStatus.Draft,
        };
    }

    public Guid RentalApplicationId { get; private set; }

    public Guid ApplicantUserId { get; private set; }

    public int RentalUnitId { get; private set; }
    public RentalUnit Unit { get; private set; } = null!;

    public ApplicantInfo? ApplicantInfo { get; private set; }

    public IReadOnlyCollection<ResidenceHistory> ResidenceHistories => _residenceHistories;
    public RentalLease? Lease { get; private set; }
    public ApplicationStatus Status { get; private set; }

    public IReadOnlyCollection<ApplicationStatusChange> History => _history;

    public string? CurrentComment =>
        _history.OrderByDescending(change => change.ChangedDate).FirstOrDefault()?.Comment;

    public bool IsEditable => ApplicationStatusRules.IsEditable(Status);

    public bool IsApplicantInfoSaved => ApplicantInfo is not null;

    public bool IsResidenceHistorySaved => _residenceHistories.Count > 0;

    public bool IsReadyToSubmit => GetIncompleteSections().Count == 0;

    public bool IsOwnedBy(Guid userId) => ApplicantUserId == userId;

    public IReadOnlyList<string> GetIncompleteSections()
    {
        var incomplete = new List<string>();
        if (!IsApplicantInfoSaved)
        {
            incomplete.Add(ApplicantInfoSection);
        }

        if (!IsResidenceHistorySaved)
        {
            incomplete.Add(ResidenceHistorySection);
        }

        return incomplete;
    }

    /// <exception cref="ApplicationNotEditableException">Thrown when the application is not in an editable status.</exception>
    public void SaveApplicantInfo(ApplicantInfo applicantInfo)
    {
        EnsureEditable();
        ApplicantInfo = applicantInfo;
    }

    /// <exception cref="ApplicationNotEditableException">Thrown when the application is not in an editable status.</exception>
    /// <exception cref="InvalidResidenceDatesException">Thrown when the move-out date precedes the move-in date.</exception>
    public ResidenceHistory AddResidence(Address address, string landlordName, string landlordPhone, DateOnly moveInDate, DateOnly? moveOutDate)
    {
        EnsureEditable();
        var residence = new ResidenceHistory(address, landlordName, landlordPhone, moveInDate, moveOutDate);
        _residenceHistories.Add(residence);
        return residence;
    }

    /// <exception cref="ApplicationNotEditableException">Thrown when the application is not in an editable status.</exception>
    /// <exception cref="ResidenceNotFoundException">Thrown when the application has no residence with the given ID.</exception>
    /// <exception cref="InvalidResidenceDatesException">Thrown when the move-out date precedes the move-in date.</exception>
    public void UpdateResidence(Guid residenceHistoryId, Address address, string landlordName, string landlordPhone, DateOnly moveInDate, DateOnly? moveOutDate)
    {
        EnsureEditable();
        var residence = FindResidence(residenceHistoryId);
        residence.Update(address, landlordName, landlordPhone, moveInDate, moveOutDate);
    }

    /// <summary>
    /// Removes the residence from the application; saving the context deletes it.
    /// </summary>
    /// <exception cref="ApplicationNotEditableException">Thrown when the application is not in an editable status.</exception>
    /// <exception cref="ResidenceNotFoundException">Thrown when the application has no residence with the given ID.</exception>
    public void RemoveResidence(Guid residenceHistoryId)
    {
        EnsureEditable();
        _residenceHistories.Remove(FindResidence(residenceHistoryId));
    }

    /// <exception cref="ApplicationNotEditableException">Thrown when the application is not in an editable status.</exception>
    /// <exception cref="ApplicationIncompleteException">Thrown when no residence has been saved.</exception>
    public void EnsureResidenceHistoryComplete()
    {
        EnsureEditable();
        if (!IsResidenceHistorySaved)
        {
            throw new ApplicationIncompleteException([ResidenceHistorySection]);
        }
    }

    /// <summary>
    /// Submits the application. Requires the unit's leases to be loaded.
    /// </summary>
    /// <exception cref="InvalidStatusTransitionException">Thrown when the application cannot be submitted from its status.</exception>
    /// <exception cref="ApplicationIncompleteException">Thrown when a section has not been saved.</exception>
    /// <exception cref="UnitNotAvailableException">Thrown when the unit has an active lease.</exception>
    public void Submit(Guid submittedBy, DateTimeOffset now, DateOnly today)
    {
        ResolveNextStatus(ApplicationAction.Submit);

        var incomplete = GetIncompleteSections();
        if (incomplete.Count > 0)
        {
            throw new ApplicationIncompleteException(incomplete);
        }

        Unit.EnsureAvailable(today);
        Apply(ApplicationAction.Submit, submittedBy, now, null);
    }

    /// <exception cref="InvalidStatusTransitionException">Thrown when the application is in a terminal status.</exception>
    public void Withdraw(Guid withdrawnBy, DateTimeOffset now) =>
        Apply(ApplicationAction.Withdraw, withdrawnBy, now, null);

    /// <exception cref="InvalidStatusTransitionException">Thrown when the application is not submitted.</exception>
    /// <exception cref="ReviewCommentRequiredException">Thrown when the comment is empty.</exception>
    public void Return(Guid reviewedBy, DateTimeOffset now, string? comment) =>
        Apply(ApplicationAction.Return, reviewedBy, now, comment);

    /// <exception cref="InvalidStatusTransitionException">Thrown when the application is not submitted.</exception>
    /// <exception cref="ReviewCommentRequiredException">Thrown when the comment is empty.</exception>
    public void Deny(Guid reviewedBy, DateTimeOffset now, string? comment) =>
        Apply(ApplicationAction.Deny, reviewedBy, now, comment);

    /// <summary>
    /// Approves the application and issues a twelve-month lease starting today. Requires the unit's leases to be loaded.
    /// </summary>
    /// <exception cref="InvalidStatusTransitionException">Thrown when the application is not submitted.</exception>
    /// <exception cref="UnitNotAvailableException">Thrown when the unit has an active lease.</exception>
    public RentalLease Approve(Guid reviewedBy, DateTimeOffset now, DateOnly today, string? comment = null)
    {
        ResolveNextStatus(ApplicationAction.Approve);

        var lease = Unit.IssueLease(this, today, today);
        Lease = lease;
        Apply(ApplicationAction.Approve, reviewedBy, now, comment);
        return lease;
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new ApplicationNotEditableException(Status);
        }
    }

    private ResidenceHistory FindResidence(Guid residenceHistoryId) =>
        _residenceHistories.FirstOrDefault(residence => residence.ResidenceHistoryId == residenceHistoryId)
        ?? throw new ResidenceNotFoundException(residenceHistoryId);

    private ApplicationStatus ResolveNextStatus(ApplicationAction action)
    {
        if (!Transitions.TryGetValue((Status, action), out ApplicationStatus nextStatus))
        {
            throw new InvalidStatusTransitionException(Status, action);
        }

        return nextStatus;
    }

    private void Apply(ApplicationAction action, Guid appliedBy, DateTimeOffset now, string? comment)
    {
        var nextStatus = ResolveNextStatus(action);

        if (ApplicationStatusRules.RequiresComment(nextStatus) && string.IsNullOrWhiteSpace(comment))
        {
            throw new ReviewCommentRequiredException(nextStatus);
        }

        var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        _history.Add(new ApplicationStatusChange(RentalApplicationId, Status, nextStatus, trimmedComment, appliedBy, now));
        Status = nextStatus;
    }
}
