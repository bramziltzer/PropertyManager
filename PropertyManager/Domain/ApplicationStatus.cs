namespace PropertyManager.Domain;

// Values are persisted as the lookup table's primary key. Never renumber or reuse a value.
public enum ApplicationStatus
{
    Draft = 1,
    Submitted = 2,
    Returned = 3,
    // terminal types:
    Approved = 4,
    Denied = 5,
    Withdrawn = 6
}
