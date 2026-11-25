using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PropertyManager.Domain;

public enum AppRole
{
    [Display(Name = "Applicant")]
    Applicant,
    [Display(Name = "Property Manager")]
    PropertyManager
}

public static class RoleNames
{
    public const string Applicant = "Applicant";
    public const string PropertyManager = "PropertyManager";
}

public static class AppRoleUtils
{
    public static List<AppRole> GetAllRoles() => Enum.GetValues<AppRole>().ToList();

    public static string GetRoleName(AppRole role)
    {
        return role switch
        {
            AppRole.Applicant => RoleNames.Applicant,
            AppRole.PropertyManager => RoleNames.PropertyManager,
            _ => throw new InvalidEnumArgumentException("Encountered unrecognized role type when fetching role name."),
        };
    }
}