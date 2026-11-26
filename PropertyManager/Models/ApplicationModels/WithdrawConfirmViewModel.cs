using PropertyManager.Domain;

namespace PropertyManager.Models.ApplicationModels;

public sealed class WithdrawConfirmViewModel
{
    public required Guid RentalApplicationId { get; init; }

    public required string PropertyName { get; init; }

    public required string UnitNumber { get; init; }

    public required ApplicationStatus Status { get; init; }
}
