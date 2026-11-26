using PropertyManager.AppService.Applications;

namespace PropertyManager.Models.ApplicationModels;

public sealed record ResidenceHistorySectionViewModel(
    Guid RentalApplicationId,
    IReadOnlyList<ResidenceDto> Residences,
    bool IsEditable);
