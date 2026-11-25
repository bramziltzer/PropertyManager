using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class RentalUnit
{
    public int RentalUnitId { get; set; }

    public string UnitNumber { get; set; } = string.Empty;

    public int PropertyId { get; set; }

    public decimal MonthlyRent { get; set; }

    public int Bedrooms { get; set; }

    public Property Property { get; set; } = null!;

    public UnitTypeKind Kind { get; set; }
    public UnitType UnitType { get; set; } = null!;

    public ICollection<RentalLease> Leases { get; set; } = [];

    public ICollection<RentalApplication> Applications { get; set; } = [];

    public RentalLease? GetActiveLease(DateOnly asOfDate)
    {
        return Leases.FirstOrDefault(l => l.BeginDate <= asOfDate && l.EndDate >= asOfDate);
    }

    public void CreateLease(RentalLease lease)
    {
        if (!LeaseRules.IsUnitAvailableForLease(this, DateOnly.FromDateTime(DateTime.UtcNow)))
        {
            throw new InvalidOperationException("Cannot create a lease for a unit that is already leased.");
        }
        Leases.Add(lease);
    }
}

