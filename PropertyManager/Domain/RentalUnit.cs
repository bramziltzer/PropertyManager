using PropertyManager.Domain.Exceptions;
using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class RentalUnit
{
    private readonly List<RentalLease> _leases = [];
    private readonly List<RentalApplication> _applications = [];

    private RentalUnit()
    {
    }

    /// <exception cref="InactiveUnitTypeException">Thrown when the unit type is inactive.</exception>
    public RentalUnit(string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        if (!unitType.IsActive)
        {
            throw new InactiveUnitTypeException(unitType.Name);
        }

        Apply(unitNumber, bedrooms, monthlyRent, unitType);
    }

    public int RentalUnitId { get; private set; }

    public string UnitNumber { get; private set; } = string.Empty;

    public int PropertyId { get; private set; }

    public decimal MonthlyRent { get; private set; }

    public int Bedrooms { get; private set; }

    public Property Property { get; private set; } = null!;

    public UnitTypeKind Kind { get; private set; }
    public UnitType UnitType { get; private set; } = null!;

    public IReadOnlyCollection<RentalLease> Leases => _leases;

    public IReadOnlyCollection<RentalApplication> Applications => _applications;

    public RentalLease? GetActiveLease(DateOnly asOfDate) => _leases.FirstOrDefault(lease => LeaseRules.IsActiveOn(lease, asOfDate));

    public bool IsAvailable(DateOnly asOfDate) => LeaseRules.IsUnitAvailableForLease(this, asOfDate);

    /// <summary>
    /// Requires <see cref="Leases"/> to be loaded.
    /// </summary>
    /// <exception cref="UnitNotAvailableException">Thrown when a lease term covers <paramref name="asOfDate"/>.</exception>
    public void EnsureAvailable(DateOnly asOfDate)
    {
        if (!IsAvailable(asOfDate))
        {
            throw new UnitNotAvailableException($"Unit {UnitNumber} has an active lease and is not available.");
        }
    }

    /// <summary>
    /// Updates the unit. A unit may keep an inactive type it already uses but cannot switch to one.
    /// </summary>
    /// <exception cref="InactiveUnitTypeException">Thrown when changing to an inactive unit type.</exception>
    internal void Update(string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        if (!unitType.IsActive && unitType.Kind != Kind)
        {
            throw new InactiveUnitTypeException(unitType.Name);
        }

        Apply(unitNumber, bedrooms, monthlyRent, unitType);
    }

    /// <summary>
    /// Issues a twelve-month lease for the application. Requires <see cref="Leases"/> to be loaded.
    /// </summary>
    /// <exception cref="UnitNotAvailableException">Thrown when the unit is leased today or the new term overlaps an existing lease.</exception>
    internal RentalLease IssueLease(RentalApplication application, DateOnly startDate, DateOnly today)
    {
        EnsureAvailable(today);

        var endDate = LeaseRules.TermEnd(startDate);
        if (_leases.Any(lease => LeaseRules.Overlaps(lease, startDate, endDate)))
        {
            throw new UnitNotAvailableException($"Unit {UnitNumber} already has a lease that overlaps the requested term.");
        }

        var lease = new RentalLease(application, this, startDate);
        _leases.Add(lease);
        return lease;
    }

    /// <summary>
    /// Requires <see cref="Leases"/> and <see cref="Applications"/> to be loaded.
    /// </summary>
    /// <exception cref="RemovalBlockedException">Thrown when the unit has applications or leases.</exception>
    internal void EnsureRemovable()
    {
        if (_leases.Count > 0 || _applications.Count > 0)
        {
            throw new RemovalBlockedException($"Unit {UnitNumber} has applications or leases and cannot be removed.");
        }
    }

    private void Apply(string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitNumber);
        ArgumentOutOfRangeException.ThrowIfNegative(bedrooms);
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyRent);

        UnitNumber = unitNumber.Trim();
        Bedrooms = bedrooms;
        MonthlyRent = monthlyRent;
        Kind = unitType.Kind;
        UnitType = unitType;
    }
}
