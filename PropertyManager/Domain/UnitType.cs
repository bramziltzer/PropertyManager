namespace PropertyManager.Domain;

public class UnitType
{
    public int UnitTypeId { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public bool IsActive(DateTime asOfDate) => EndDate is null || asOfDate.ToUniversalTime() < EndDate?.ToUniversalTime();
}
