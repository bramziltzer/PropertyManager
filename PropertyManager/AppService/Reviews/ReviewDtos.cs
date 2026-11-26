using PropertyManager.Domain;

namespace PropertyManager.AppService.Reviews;

public enum ReviewOutcome
{
    Approve,
    Return,
    Deny
}

public sealed record ReviewDetailsDto(
    Guid RentalApplicationId,
    ApplicationStatus Status,
    string PropertyName,
    string UnitNumber,
    string ApplicantDisplayName);
