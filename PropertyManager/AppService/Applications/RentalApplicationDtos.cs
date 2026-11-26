using PropertyManager.Domain;

namespace PropertyManager.AppService.Applications;

public sealed record ApplicationListFilter(
    ApplicationStatus? Status = null,
    int? PropertyId = null);

 public sealed record ApplicationListItemDto(
    Guid RentalApplicationId,
    int PropertyId,
    string PropertyName,
    int RentalUnitId,
    string UnitNumber,
    decimal MonthlyRent,
    ApplicationStatus Status,
    string ApplicantDisplayName,
    bool IsEditable);

public sealed record ApplicationListResultDto(
    IReadOnlyList<ApplicationListItemDto> Applications,
    IReadOnlyList<PropertyFilterOptionDto> PropertyFilterOptions,
    bool IsPropertyManager);
