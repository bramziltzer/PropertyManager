using PropertyManager.Domain.Exceptions;
using PropertyManager.Domain.Rules;

namespace PropertyManager.Domain;

public class ResidenceHistory
{
    private ResidenceHistory()
    {
    }

    internal ResidenceHistory(Address address, string landlordName, string landlordPhone, DateOnly moveInDate, DateOnly? moveOutDate, DateOnly today)
    {
        Update(address, landlordName, landlordPhone, moveInDate, moveOutDate, today);
    }

    public Guid ResidenceHistoryId { get; private set; }
    public Guid RentalApplicationId { get; private set; }
    public Address Address { get; private set; } = null!;
    public string LandlordName { get; private set; } = string.Empty;
    public string LandlordPhone { get; private set; } = string.Empty;
    public DateOnly MoveInDate { get; private set; }
    public DateOnly? MoveOutDate { get; private set; }

    public RentalApplication RentalApplication { get; private set; } = null!;

    internal void Update(Address address, string landlordName, string landlordPhone, DateOnly moveInDate, DateOnly? moveOutDate, DateOnly today)
    {
        if (moveOutDate is not null && moveOutDate < moveInDate)
        {
            throw new InvalidResidenceDatesException();
        }

        if (moveInDate > today || moveOutDate > today)
        {
            throw new FutureResidenceDateException();
        }

        if (!PhoneNumberRules.IsValid(landlordPhone))
        {
            throw new InvalidPhoneNumberException();
        }

        Address = address;
        LandlordName = landlordName.Trim();
        LandlordPhone = landlordPhone.Trim();
        MoveInDate = moveInDate;
        MoveOutDate = moveOutDate;
    }
}
