using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class RentalUnit
{
    public Guid RentalUnitId { get; set; }
    
    public string UnitNumber { get; set; }

    public Guid PropertyId { get; set; }

    public decimal RentAmount { get; set; }

    public int BedroomCount { get; set; }
    
    // navigation property
    public Property Property { get; set; }

    public int UnitTypeId { get; set; }
    // navigation property
    public UnitType UnitType { get; set; }

    // ef navigation property
    public List<Lease> Leases { get; set; } = new List<Lease>();

    public Lease? GetActiveLease(DateTime asOfDate)
    {
        return Leases.FirstOrDefault(l => l.BeginDate <= asOfDate && l.EndDate >= asOfDate);
    }

    // TODO this may not belong here
    public void CreateLease(Lease lease)
    {
        // TODO creates lease with 12 month term but should be able to create leases with different terms
        if (!LeaseRules.IsUnitAvailableForLease(this, DateTime.UtcNow))
        {
            throw new InvalidOperationException("Cannot create a lease for a unit that is already leased.");
        }
        Leases.Add(lease);
    }
}

