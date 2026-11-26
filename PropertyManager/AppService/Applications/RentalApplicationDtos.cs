using PropertyManager.Domain;

namespace PropertyManager.AppService.Applications;

public enum ApplicationSection
{
    ApplicantInfo,
    ResidenceHistory,
    Summary
}

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

public sealed record UnitSummaryDto(
    int RentalUnitId,
    int PropertyId,
    string PropertyName,
    AddressDto PropertyAddress,
    string UnitNumber,
    int Bedrooms,
    decimal MonthlyRent,
    string UnitTypeName);

public sealed record ApplicantInfoDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Email,
    AddressDto CurrentAddress);

public sealed record SaveApplicantInfoDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Email,
    AddressDto CurrentAddress);

public sealed record ResidenceListDto(
    Guid RentalApplicationId,
    IReadOnlyList<ResidenceDto> Residences,
    ApplicationStatus Status,
    bool IsEditable);

public sealed record ResidenceDto(
    Guid ResidenceHistoryId,
    AddressDto Address,
    string LandlordName,
    string LandlordPhone,
    DateOnly MoveInDate,
    DateOnly? MoveOutDate);

public sealed record SaveResidenceDto(
    AddressDto Address,
    string LandlordName,
    string LandlordPhone,
    DateOnly MoveInDate,
    DateOnly? MoveOutDate);

public sealed record ApplicationStatusChangeDto(
    Guid ApplicationStatusChangeId,
    ApplicationStatus OldStatus,
    ApplicationStatus NewStatus,
    string? Comment,
    string ChangedByDisplayName,
    DateTimeOffset ChangedDate);

public sealed record ApplicationPageDto(
    Guid RentalApplicationId,
    ApplicationStatus Status,
    ApplicationSection CurrentSection,
    UnitSummaryDto Unit,
    ApplicantInfoDto? ApplicantInfo,
    IReadOnlyList<ResidenceDto> Residences,
    bool IsApplicantInfoSaved,
    bool IsResidenceHistorySaved,
    bool IsReadyToSubmit,
    IReadOnlyList<ApplicationSection> IncompleteSections,
    bool IsEditable,
    bool IsPropertyManager,
    IReadOnlyList<ApplicationSection> AllowedSections,
    string? CurrentComment,
    IReadOnlyList<ApplicationStatusChangeDto>? History);
