using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.AppService.Reviews;

public sealed class ReviewService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IReviewService
{
    public async Task<ReviewDetailsDto> GetReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        return await (
            from app in dbContext.RentalApplications.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on app.ApplicantUserId equals user.Id
            where app.RentalApplicationId == applicationId
            select new ReviewDetailsDto(
                app.RentalApplicationId,
                app.Status,
                app.Unit.Property.Name,
                app.Unit.UnitNumber,
                app.ApplicantInfo != null
                    ? app.ApplicantInfo.FirstName + " " + app.ApplicantInfo.LastName
                    : (user.Email ?? user.UserName ?? "Applicant")))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");
    }

    public async Task ReviewAsync(
        Guid applicationId,
        ReviewOutcome outcome,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var application = await dbContext.RentalApplications
            .Include(app => app.Unit)
                .ThenInclude(unit => unit.Leases)
            .FirstOrDefaultAsync(app => app.RentalApplicationId == applicationId, cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        switch (outcome)
        {
            case ReviewOutcome.Approve:
                application.Approve(currentUser.Id, now, today, comment);
                dbContext.Entry(application.Unit).Property(unit => unit.UnitNumber).IsModified = true;
                break;
            case ReviewOutcome.Return:
                application.Return(currentUser.Id, now, comment);
                break;
            case ReviewOutcome.Deny:
                application.Deny(currentUser.Id, now, comment);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unsupported review outcome.");
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConcurrencyException(
                "The application or unit was modified by another user. Please reload and try again.",
                ex);
        }
    }

    public async Task<IReadOnlyList<ApplicationStatusChangeDto>> GetHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        EnsurePropertyManager();

        var exists = await dbContext.RentalApplications
            .AsNoTracking()
            .AnyAsync(app => app.RentalApplicationId == applicationId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException($"Application {applicationId} was not found.");
        }

        var history = await (
            from change in dbContext.ApplicationStatusChanges.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on change.ChangedByUserId equals user.Id
            where change.RentalApplicationId == applicationId
            select new ApplicationStatusChangeDto(
                change.ApplicationStatusChangeId,
                change.OldStatus,
                change.NewStatus,
                change.Comment,
                user.Email ?? user.UserName ?? "User",
                change.ChangedDate))
            .ToListAsync(cancellationToken);

        return history
            .OrderByDescending(item => item.ChangedDate)
            .ToList();
    }

    private void EnsurePropertyManager()
    {
        if (!currentUser.IsPropertyManager)
        {
            throw new ForbiddenException("Only property managers can review rental applications.");
        }
    }
}
