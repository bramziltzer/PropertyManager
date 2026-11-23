namespace PropertyManager.Domain;

public enum ApplicationStatus
{
    Draft,
    Submitted,
    Returned,
    // terminal types:
    Approved,
    Denied,
    Withdrawn
}
