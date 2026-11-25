namespace PropertyManager.Domain;

public class ResidenceHistory
{
    public Guid ResidenceHistoryId { get; set; }
    public Guid RentalApplicationId { get; set; }
    public Address Address { get; set; } = null!;
    public string LandlordName { get; set; } = string.Empty;
    public string LandlordPhone { get; set; } = string.Empty;
    public DateOnly MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }

    public RentalApplication RentalApplication { get; set; } = null!;
}