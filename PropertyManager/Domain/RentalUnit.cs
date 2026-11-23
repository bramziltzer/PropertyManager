using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class RentalUnit
{
    public Guid RentalUnitId { get; set; }
    
    string? UnitName { get; set; }

    public Guid PropertyId { get; set; }
    
    // navigation property
    public RentalProperty Property { get; set; }

    public int UnitTypeId { get; set; }
    // navigation property
    public UnitType UnitType { get; set; }

    // ef navigation property
    public List<RentalLease> Leases { get; set; } = new List<RentalLease>();

    public RentalLease? GetActiveLease(DateTime asOfDate)
    {
        return Leases.FirstOrDefault(l => l.BeginDate <= asOfDate && l.EndDate >= asOfDate);
    }

    // TODO this may not belong here
    public void CreateLease(RentalLease lease)
    {
        // TODO creates lease with 12 month term but should be able to create leases with different terms
        if (!LeaseRules.IsUnitAvailableForLease(this, DateTime.UtcNow))
        {
            throw new InvalidOperationException("Cannot create a lease for a unit that is already leased.");
        }
        Leases.Add(lease);
    }
}

