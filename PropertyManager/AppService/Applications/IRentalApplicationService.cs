namespace PropertyManager.AppService.Applications;

public interface IRentalApplicationService
{
    Task<Guid> StartApplicationAsync(int unitId, CancellationToken cancellationToken = default);

    Task<ApplicationListResultDto> GetApplicationsAsync(
        ApplicationListFilter filter,
        CancellationToken cancellationToken = default);

    Task<ApplicationPageDto> GetApplicationPageAsync(
        Guid applicationId,
        ApplicationSection? requestedSection = null,
        CancellationToken cancellationToken = default);

    Task SaveApplicantInfoAsync(
        Guid applicationId,
        SaveApplicantInfoDto dto,
        CancellationToken cancellationToken = default);

    Task<ResidenceListDto> GetApplicationResidencesAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<ResidenceDto> GetResidenceAsync(
        Guid applicationId,
        Guid residenceHistoryId,
        CancellationToken cancellationToken = default);

    Task<Guid> AddResidenceAsync(
        Guid applicationId,
        SaveResidenceDto dto,
        CancellationToken cancellationToken = default);

    Task UpdateResidenceAsync(
        Guid applicationId,
        Guid residenceHistoryId,
        SaveResidenceDto dto,
        CancellationToken cancellationToken = default);

    Task RemoveResidenceAsync(
        Guid applicationId,
        Guid residenceHistoryId,
        CancellationToken cancellationToken = default);

    Task EnsureResidenceHistoryCompleteAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task SubmitAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task WithdrawAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);
}
