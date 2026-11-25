using PropertyManager.Domain;

namespace PropertyManager.Tests;

internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 6, 1);
    public static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    public static readonly Guid ApplicantId = Guid.NewGuid();
    public static readonly Guid ManagerId = Guid.NewGuid();

    public static Address Address() => new("1 Main St", null, "Springfield", "IL", "62701");

    public static UnitType ActiveType(UnitTypeKind kind = UnitTypeKind.Apartment) =>
        new() { Kind = kind, Name = kind.ToString(), IsActive = true };

    public static UnitType InactiveType(UnitTypeKind kind = UnitTypeKind.Townhouse) =>
        new() { Kind = kind, Name = kind.ToString(), IsActive = false };

    public static RentalUnit Unit(string number = "101") => new(number, 2, 1500m, ActiveType());

    public static RentalApplication Draft(RentalUnit? unit = null, Guid? applicantId = null) =>
        RentalApplication.StartDraft(unit ?? Unit(), applicantId ?? ApplicantId, Today);

    public static ApplicantInfo Info() => new("Ada", "Lovelace", "555-0100", "ada@example.com", Address());

    public static RentalApplication Completed(RentalUnit? unit = null, Guid? applicantId = null)
    {
        var application = Draft(unit, applicantId);
        application.SaveApplicantInfo(Info());
        application.AddResidence(Address(), "Landlord", "555-0101", new DateOnly(2020, 1, 1), null);
        return application;
    }

    public static RentalApplication Submitted(RentalUnit? unit = null, Guid? applicantId = null)
    {
        var application = Completed(unit, applicantId);
        application.Submit(applicantId ?? ApplicantId, Now, Today);
        return application;
    }

    public static RentalApplication InStatus(ApplicationStatus status)
    {
        var application = status == ApplicationStatus.Draft ? Draft() : Submitted();
        switch (status)
        {
            case ApplicationStatus.Returned:
                application.Return(ManagerId, Now, "Fix it.");
                break;
            case ApplicationStatus.Approved:
                application.Approve(ManagerId, Now, Today);
                break;
            case ApplicationStatus.Denied:
                application.Deny(ManagerId, Now, "No.");
                break;
            case ApplicationStatus.Withdrawn:
                application.Withdraw(ApplicantId, Now);
                break;
        }

        return application;
    }
}
