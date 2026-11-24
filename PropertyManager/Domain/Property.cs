namespace PropertyManager.Domain;

public class Property
{
    public int PropertyId { get; set; }
    public Address Address { get; set; }    

    // ef navigation property
    public ICollection<RentalUnit> Units { get; set; } = [];

    public void AddUnit(RentalUnit unit) => Units.Add(unit);
    public void RemoveUnit(RentalUnit unit) => Units.Remove(unit);
}
