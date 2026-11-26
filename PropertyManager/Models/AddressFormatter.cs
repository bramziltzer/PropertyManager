using PropertyManager.AppService;

namespace PropertyManager.Models;

public static class AddressFormatter
{
    public static string Format(AddressDto address)
    {
        var street = string.IsNullOrWhiteSpace(address.Line2)
            ? address.Line1
            : $"{address.Line1}, {address.Line2}";

        return $"{street}, {address.City}, {address.State} {address.ZipCode}";
    }
}
