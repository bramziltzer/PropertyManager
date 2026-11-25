namespace PropertyManager.Domain;

public class UnitType
{
    public UnitTypeKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public bool IsActive(DateOnly asOfDate) => EndDate is null || asOfDate < EndDate;
}
