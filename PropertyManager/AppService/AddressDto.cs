using PropertyManager.Domain;

namespace PropertyManager.AppService;

/// <summary>
/// Represents a postal address.
/// </summary>
/// <param name="Line1"></param>
/// <param name="Line2"></param>
/// <param name="City"></param>
/// <param name="State"></param>
/// <param name="ZipCode"></param>
public sealed record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string State,
    string ZipCode)
{
    public Address ToDomainAddress()
    {
        return new Address(
            Line1.Trim(),
            string.IsNullOrWhiteSpace(Line2) ? null : Line2.Trim(),
            City.Trim(),
            State.Trim(),
            ZipCode.Trim());
    }
}