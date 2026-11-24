namespace PropertyManager.Domain;

public class Lease
{
    public Guid LeaseId { get; set; }

    public Guid LesseeId { get; set; }

    // TODO maybe remove this?
    public Guid? AssociatedApplicationId { get; set; }

    // navigation property
    public RentalApplication? AssociatedApplication { get; set; }

    public Guid RentalUnitId { get; set; }

    // navigation property
    public RentalUnit RentalUnit { get; set; }

    public DateTime BeginDate { get; set; }

    public DateTime EndDate { get; set; }
}