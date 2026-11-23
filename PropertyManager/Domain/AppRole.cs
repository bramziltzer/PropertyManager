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

public static class AppRoleUtils
{
    public static List<AppRole> GetAllRoles() => Enum.GetValues<AppRole>().ToList();

    // TODO remove
    public static string GetRoleName(AppRole role)
    {
        return role switch
        {
            AppRole.Applicant => "Applicant",
            AppRole.PropertyManager => "Property Manager",
            _ => throw new InvalidEnumArgumentException("Encountered unrecognized role type when fetching role name."),
        };
    }
}