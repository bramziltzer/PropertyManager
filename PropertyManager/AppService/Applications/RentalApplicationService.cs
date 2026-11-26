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
    public async Task<Guid> StartApplicationAsync(int unitId, CancellationToken cancellationToken = default)
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

    public async Task<ApplicationListResultDto> GetApplicationsAsync(
        ApplicationListFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var isManager = currentUser.IsPropertyManager;
        if (!isManager && !currentUser.IsApplicant)
        {
            throw new ForbiddenException("You do not have permission to view rental applications.");
        }

        var query = dbContext.RentalApplications
            .AsNoTracking();

        // Apply applicant filter for non-managers
        if (!isManager)
        {
            var applicantId = currentUser.Id;
            query = query.Where(app => app.ApplicantUserId == applicantId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(app => app.Status == filter.Status.Value);
        }

        if (filter.PropertyId.HasValue)
        {
            query = query.Where(app => app.Unit.PropertyId == filter.PropertyId.Value);
        }

        var applications = await (
            from app in query
            join user in dbContext.Users.AsNoTracking() on app.ApplicantUserId equals user.Id
            orderby app.Unit.Property.Name, app.Unit.UnitNumber, app.RentalApplicationId
            select new ApplicationListItemDto(
                app.RentalApplicationId,
                app.Unit.PropertyId,
                app.Unit.Property.Name,
                app.RentalUnitId,
                app.Unit.UnitNumber,
                app.Unit.MonthlyRent,
                app.Status,
                app.ApplicantInfo != null
                    ? app.ApplicantInfo.FirstName + " " + app.ApplicantInfo.LastName
                    : (user.Email ?? user.UserName ?? "Applicant"),
                app.Status == ApplicationStatus.Draft || app.Status == ApplicationStatus.Returned))
            .ToListAsync(cancellationToken);

        var propertyFilterOptions = await dbContext.GetPropertyFilterOptionsAsync(cancellationToken);

        return new ApplicationListResultDto(applications, propertyFilterOptions, isManager);
    }
}
