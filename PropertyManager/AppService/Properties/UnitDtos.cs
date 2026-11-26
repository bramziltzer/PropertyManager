using PropertyManager.Domain;

namespace PropertyManager.AppService.Properties;

/// <summary>
/// Represents a rental unit within a property.
/// </summary>
public sealed record UnitDto(
    int RentalUnitId,
    int PropertyId,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    UnitTypeKind Kind,
    string UnitTypeName,
    bool IsUnitTypeActive,
    bool HasApplicationsOrLeases);

/// <summary>
/// Represents a selectable unit type option.
/// </summary>
public sealed record UnitTypeOptionDto(
    UnitTypeKind Kind,
    string Name,
    bool IsActive);

/// <summary>
/// Represents a request to create or update a rental unit.
/// </summary>
public sealed record SaveUnitRequest(
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    UnitTypeKind Kind);
