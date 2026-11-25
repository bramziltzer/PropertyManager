using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class RentalLease
{
    private RentalLease()
    {
    }

    internal RentalLease(RentalApplication application, RentalUnit unit, DateOnly startDate)
    {
        RentalApplication = application;
        RentalUnit = unit;
        LesseeId = application.ApplicantUserId;
        BeginDate = startDate;
        EndDate = LeaseRules.TermEnd(startDate);
    }

    public Guid LeaseId { get; private set; }

    public Guid LesseeId { get; private set; }

    public ApplicationUser Lessee { get; private set; } = null!;

    public Guid RentalApplicationId { get; private set; }

    public RentalApplication RentalApplication { get; private set; } = null!;

    public int RentalUnitId { get; private set; }

    public RentalUnit RentalUnit { get; private set; } = null!;

    public DateOnly BeginDate { get; private set; }

    public DateOnly EndDate { get; private set; }
}
