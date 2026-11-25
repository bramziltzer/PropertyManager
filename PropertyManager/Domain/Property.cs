using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Domain;

public class Property
{
    private readonly List<RentalUnit> _units = [];

    private Property()
    {
    }

    public Property(string name, Address address)
    {
        Update(name, address);
    }

    public int PropertyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;

    public IReadOnlyCollection<RentalUnit> Units => _units;

    public void Update(string name, Address address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Address = address;
    }

    /// <summary>
    /// Requires <see cref="Units"/> to be loaded.
    /// </summary>
    /// <exception cref="DuplicateUnitNumberException">Thrown when the property already has a unit with the same number.</exception>
    public void AddUnit(RentalUnit unit)
    {
        EnsureUnitNumberIsFree(unit.UnitNumber, except: null);
        _units.Add(unit);
    }

    /// <exception cref="DuplicateUnitNumberException">Thrown when another unit already has the new number.</exception>
    /// <exception cref="InactiveUnitTypeException">Thrown when changing to an inactive unit type.</exception>
    public void UpdateUnit(RentalUnit unit, string unitNumber, int bedrooms, decimal monthlyRent, UnitType unitType)
    {
        EnsureUnitNumberIsFree(unitNumber, except: unit);
        unit.Update(unitNumber, bedrooms, monthlyRent, unitType);
    }

    /// <summary>
    /// Removes the unit from the property; saving the context deletes it.
    /// </summary>
    /// <exception cref="RemovalBlockedException">Thrown when the unit has applications or leases.</exception>
    public void RemoveUnit(RentalUnit unit)
    {
        unit.EnsureRemovable();
        _units.Remove(unit);
    }

    /// <exception cref="RemovalBlockedException">Thrown when the property still has units.</exception>
    public void EnsureRemovable()
    {
        if (_units.Count > 0)
        {
            throw new RemovalBlockedException($"Property {Name} still has units and cannot be removed.");
        }
    }

    private void EnsureUnitNumberIsFree(string unitNumber, RentalUnit? except)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitNumber);
        var number = unitNumber.Trim();
        if (_units.Any(unit => unit != except && string.Equals(unit.UnitNumber, number, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateUnitNumberException(number);
        }
    }
}
