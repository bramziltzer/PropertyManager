namespace PropertyManager.Domain;

public class Property
{
    public int PropertyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;

    public ICollection<RentalUnit> Units { get; set; } = [];

    public void AddUnit(RentalUnit unit) => Units.Add(unit);
    public void RemoveUnit(RentalUnit unit) => Units.Remove(unit);
}
