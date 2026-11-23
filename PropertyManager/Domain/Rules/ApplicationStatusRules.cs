namespace PropertyManager.Domain;

public static class ApplicationStatusRules
{
    public static bool RequiresComment(ApplicationStatus status) =>
        status is ApplicationStatus.Returned or ApplicationStatus.Denied;
}