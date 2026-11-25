using Microsoft.AspNetCore.Identity;

namespace PropertyManager.Domain;

public class RentalLease
{
    public Guid LeaseId { get; set; }

    public string LesseeId { get; set; } = string.Empty;
    
    public IdentityUser Lessee { get; set; } = null!;

    public Guid RentalApplicationId { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;

    public Guid RentalUnitId { get; set; }

    public RentalUnit RentalUnit { get; set; } = null!;

    public DateTime BeginDate { get; set; }

    public DateTime EndDate { get; set; }
}