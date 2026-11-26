namespace PropertyManager.AppService;

/// <summary>
/// Represents a property option for filter dropdowns.
/// </summary>
/// <param name="PropertyId">The unique identifier of the property.</param>
/// <param name="Name">The display name of the property.</param>
public sealed record PropertyFilterOptionDto(
    int PropertyId,
    string Name);
