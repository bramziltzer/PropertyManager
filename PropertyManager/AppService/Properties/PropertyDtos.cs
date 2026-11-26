namespace PropertyManager.AppService.Properties;

/// <summary>
/// Represents a summary of a property, including its address and number of units.
/// </summary>
/// <param name="PropertyId"></param>
/// <param name="Name"></param>
/// <param name="Address"></param>
/// <param name="UnitCount"></param>
public sealed record PropertySummaryDto(
    int PropertyId,
    string Name,
    AddressDto Address,
    int UnitCount);

/// <summary>
/// Represents detailed information about a property, including its address and number of units.
/// </summary>
/// <param name="PropertyId"></param>
/// <param name="Name"></param>
/// <param name="Address"></param>
/// <param name="UnitCount"></param>
public sealed record PropertyDetailDto(
    int PropertyId,
    string Name,
    AddressDto Address,
    int UnitCount);

/// <summary>
/// Represents a request to save a property, including its name and address.
/// </summary>
/// <param name="Name"></param>
/// <param name="Address"></param>
public sealed record SavePropertyRequest(
    string Name,
    AddressDto Address);
