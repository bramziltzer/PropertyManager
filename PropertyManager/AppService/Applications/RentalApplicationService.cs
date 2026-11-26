using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.AppService.Applications;

public sealed class RentalApplicationService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRentalApplicationService
{
    public async Task<Guid> StartAsync(int unitId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsApplicant)
        {
            throw new ForbiddenException("Only applicants can start a rental application.");
        }

        var applicantId = currentUser.Id;

        var unit = await dbContext.RentalUnits
            .Include(item => item.Leases)
            .FirstOrDefaultAsync(item => item.RentalUnitId == unitId, cancellationToken)
            ?? throw new NotFoundException($"Unit {unitId} was not found.");

        var hasOpenApplication = await dbContext.RentalApplications
            .AnyAsync(
                app => app.ApplicantUserId == applicantId
                    && app.RentalUnitId == unitId
                    && ApplicationStatusRules.OpenStatuses.Contains(app.Status),
                cancellationToken);

        if (hasOpenApplication)
        {
            throw new DuplicateOpenApplicationException();
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var application = RentalApplication.StartDraft(unit, applicantId, today);

        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken);

        return application.RentalApplicationId;
    }
}
