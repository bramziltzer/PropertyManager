namespace PropertyManager.Domain;

public class RentalProperty
{
    // ef constructor
    private RentalProperty() {}

    // ef navigation property
    public ICollection<RentalUnit> RentalUnits { get; set; } = [];

    public void AddUnit(RentalUnit unit) => RentalUnits.Add(unit);
    public void RemoveUnit(RentalUnit unit) => RentalUnits.Remove(unit);
}
