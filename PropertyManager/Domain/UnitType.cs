namespace PropertyManager.Domain;

public class UnitType
{
    public UnitTypeKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
