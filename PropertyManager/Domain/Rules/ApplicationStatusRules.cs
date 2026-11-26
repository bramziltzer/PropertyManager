namespace PropertyManager.Domain;

public static class ApplicationStatusRules
{
    public static readonly HashSet<ApplicationStatus> OpenStatuses =
    [
        ApplicationStatus.Draft,
        ApplicationStatus.Submitted,
        ApplicationStatus.Returned
    ];

    public static bool RequiresComment(ApplicationStatus status) =>
        status is ApplicationStatus.Returned or ApplicationStatus.Denied;

    public static bool IsEditable(ApplicationStatus status) =>
        status is ApplicationStatus.Draft or ApplicationStatus.Returned;

    public static bool IsTerminal(ApplicationStatus status) =>
        status is ApplicationStatus.Approved or ApplicationStatus.Denied or ApplicationStatus.Withdrawn;
}