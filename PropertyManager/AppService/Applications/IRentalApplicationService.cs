namespace PropertyManager.AppService.Applications;

public interface IRentalApplicationService
{
    Task<Guid> StartApplicationAsync(int unitId, CancellationToken cancellationToken = default);

    Task<ApplicationListResultDto> GetApplicationsAsync(
        ApplicationListFilter filter,
        CancellationToken cancellationToken = default);
}
