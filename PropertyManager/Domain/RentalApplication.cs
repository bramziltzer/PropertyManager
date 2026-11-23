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

    /// <summary>
    /// Updates the application status and property manager comments.
    /// </summary>
    /// <param name="newStatus">The new application status.</param>
    /// <param name="propertyManagerComments">Comments from the property manager.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newStatus"/> is <see cref="ApplicationStatus.Returned"/> or <see
    /// cref="ApplicationStatus.Denied"/> and <paramref name="propertyManagerComments"/> is null, empty, or whitespace.</exception>
    public void UpdateStatus(ApplicationStatus newStatus, string? propertyManagerComments = null)
    {
        // TODO transition check
        if (ApplicationStatusRules.RequiresComment(newStatus) && string.IsNullOrWhiteSpace(propertyManagerComments))
        {
            throw new ArgumentException("Property manager comments are required for the new application status {0}.", nameof(newStatus));
        }
        Status = newStatus;
        PropertyManagerComments = propertyManagerComments;
        // TODO create history audit (who, when, comment) => need some kind of history table then...
        // replace status and comments properties with a status collection with date and user
    }
}
