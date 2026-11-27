using PropertyManager.AppService;
using PropertyManager.AppService.Applications;
using PropertyManager.Domain;

namespace PropertyManager.Models.ApplicationModels;

public sealed class ApplicationPageViewModel
{
    public Guid RentalApplicationId { get; set; }

    public ApplicationStatus Status { get; set; }

    public ApplicationSection CurrentSection { get; set; }

    public UnitSummaryViewModel Unit { get; set; } = new();

    public bool IsEditable { get; set; }

    public bool IsReadOnly => !IsEditable;

    public bool IsPropertyManager { get; set; }

    public bool IsApplicantInfoSaved { get; set; }

    public bool IsResidenceHistorySaved { get; set; }

    public bool IsReadyToSubmit { get; set; }

    public bool IsApplicant { get; set; }

    public bool CanSubmit => IsEditable && CurrentSection == ApplicationSection.Summary && IsReadyToSubmit;

    public bool CanWithdraw => !IsPropertyManager && ApplicationStatusRules.OpenStatuses.Contains(Status);

    public bool CanReview => IsPropertyManager && Status == ApplicationStatus.Submitted;

    public IReadOnlyList<ApplicationSection> IncompleteSections { get; set; } = [];

    public IReadOnlyList<ApplicationSection> AllowedSections { get; set; } = [];

    public string? CurrentComment { get; set; }

    public ApplicantInfoSectionViewModel ApplicantInfo { get; set; } = new();

    public IReadOnlyList<ResidenceDto> Residences { get; set; } = [];

    public IReadOnlyList<ApplicationStatusChangeViewModel>? History { get; set; }

    public static ApplicationPageViewModel FromDto(ApplicationPageDto dto) => new()
    {
        RentalApplicationId = dto.RentalApplicationId,
        Status = dto.Status,
        CurrentSection = dto.CurrentSection,
        Unit = UnitSummaryViewModel.FromDto(dto.Unit),
        IsEditable = dto.IsEditable,
        IsPropertyManager = dto.IsPropertyManager,
        IsApplicant = dto.IsApplicant,
        IsApplicantInfoSaved = dto.IsApplicantInfoSaved,
        IsResidenceHistorySaved = dto.IsResidenceHistorySaved,
        IsReadyToSubmit = dto.IsReadyToSubmit,
        IncompleteSections = dto.IncompleteSections,
        AllowedSections = dto.AllowedSections,
        CurrentComment = dto.CurrentComment,
        ApplicantInfo = ApplicantInfoSectionViewModel.FromDto(dto.ApplicantInfo, !dto.IsEditable),
        Residences = dto.Residences,
        History = dto.History?
            .Select(ApplicationStatusChangeViewModel.FromDto)
            .ToList()
    };

    public void PopulatePageState(ApplicationPageDto dto, ApplicationSection currentSection)
    {
        RentalApplicationId = dto.RentalApplicationId;
        Status = dto.Status;
        CurrentSection = currentSection;
        Unit = UnitSummaryViewModel.FromDto(dto.Unit);
        IsEditable = dto.IsEditable;
        IsPropertyManager = dto.IsPropertyManager;
        IsApplicant = dto.IsApplicant;
        IsApplicantInfoSaved = dto.IsApplicantInfoSaved;
        IsResidenceHistorySaved = dto.IsResidenceHistorySaved;
        IsReadyToSubmit = dto.IsReadyToSubmit;
        IncompleteSections = dto.IncompleteSections;
        AllowedSections = dto.AllowedSections;
        CurrentComment = dto.CurrentComment;
        ApplicantInfo.IsReadOnly = !dto.IsEditable;
        Residences = dto.Residences;
        History = dto.History?
            .Select(ApplicationStatusChangeViewModel.FromDto)
            .ToList();
    }
}

public sealed class UnitSummaryViewModel
{
    public int RentalUnitId { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string FormattedAddress { get; set; } = string.Empty;
    public string UnitNumber { get; set; } = string.Empty;
    public int Bedrooms { get; set; }
    public decimal MonthlyRent { get; set; }
    public string UnitTypeName { get; set; } = string.Empty;

    public static UnitSummaryViewModel FromDto(UnitSummaryDto dto) => new()
    {
        RentalUnitId = dto.RentalUnitId,
        PropertyId = dto.PropertyId,
        PropertyName = dto.PropertyName,
        FormattedAddress = AddressFormatter.Format(dto.PropertyAddress),
        UnitNumber = dto.UnitNumber,
        Bedrooms = dto.Bedrooms,
        MonthlyRent = dto.MonthlyRent,
        UnitTypeName = dto.UnitTypeName
    };
}
