namespace PropertyManager.Domain;

public class RentalLease
{
    public Guid LeaseId { get; set; }

    public Guid LesseeId { get; set; }

    public ApplicationUser Lessee { get; set; } = null!;

    public Guid RentalApplicationId { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public int RentalUnitId { get; set; }

    public RentalUnit RentalUnit { get; set; } = null!;

    public DateOnly BeginDate { get; set; }

    public DateOnly EndDate { get; set; }
}