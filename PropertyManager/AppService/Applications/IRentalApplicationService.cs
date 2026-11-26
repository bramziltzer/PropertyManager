namespace PropertyManager.AppService.Applications;

public interface IRentalApplicationService
{
    Task<Guid> StartAsync(int unitId, CancellationToken cancellationToken = default);
}
