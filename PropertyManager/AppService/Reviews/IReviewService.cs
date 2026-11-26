using PropertyManager.AppService.Applications;

namespace PropertyManager.AppService.Reviews;

public interface IReviewService
{
    Task<ReviewDetailsDto> GetReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task ReviewAsync(
        Guid applicationId,
        ReviewOutcome outcome,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApplicationStatusChangeDto>> GetHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
