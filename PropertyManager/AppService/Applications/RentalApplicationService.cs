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
        EnsureCanViewApplications();

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

    public async Task<ApplicationPageDto> GetApplicationPageAsync(
        Guid applicationId,
        ApplicationSection? requestedSection = null,
        CancellationToken cancellationToken = default)
    {
        EnsureCanViewApplications();

        var isManager = currentUser.IsPropertyManager;
        var pageData = await LoadApplicationPageDataAsync(applicationId, cancellationToken);
        var state = BuildApplicationPageState(pageData, isManager);
        var history = isManager
            ? await LoadApplicationHistoryAsync(applicationId, cancellationToken)
            : null;

        return new ApplicationPageDto(
            pageData.ApplicationId,
            pageData.Status,
            ResolveCurrentSection(requestedSection, state),
            pageData.Unit,
            pageData.ApplicantInfo,
            pageData.Residences,
            state.IsApplicantInfoSaved,
            state.IsResidenceHistorySaved,
            state.IsReadyToSubmit,
            state.IncompleteSections,
            state.IsEditable,
            isManager,
            state.AllowedSections,
            GetCurrentComment(pageData.HistoryEntries),
            history);
    }

    private async Task<ApplicationPageData> LoadApplicationPageDataAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.RentalApplications
            .AsNoTracking()
            .Where(app => app.RentalApplicationId == applicationId);

        if (!currentUser.IsPropertyManager)
        {
            var applicantId = currentUser.Id;
            query = query.Where(app => app.ApplicantUserId == applicantId);
        }

        return await query
            .Select(app => new ApplicationPageData(
                app.RentalApplicationId,
                app.Status,
                new UnitSummaryDto(
                    app.Unit.RentalUnitId,
                    app.Unit.PropertyId,
                    app.Unit.Property.Name,
                    AddressDto.FromDomainAddress(app.Unit.Property.Address),
                    app.Unit.UnitNumber,
                    app.Unit.Bedrooms,
                    app.Unit.MonthlyRent,
                    app.Unit.UnitType.Name),
                app.ApplicantInfo == null
                    ? null
                    : new ApplicantInfoDto(
                        app.ApplicantInfo.FirstName,
                        app.ApplicantInfo.LastName,
                        app.ApplicantInfo.PhoneNumber,
                        app.ApplicantInfo.Email,
                        AddressDto.FromDomainAddress(app.ApplicantInfo.CurrentAddress)),
                app.ResidenceHistories
                    .OrderByDescending(r => r.MoveInDate)
                    .Select(r => new ResidenceDto(
                        r.ResidenceHistoryId,
                        AddressDto.FromDomainAddress(r.Address),
                        r.LandlordName,
                        r.LandlordPhone,
                        r.MoveInDate,
                        r.MoveOutDate))
                    .ToList(),
                app.History
                    .Select(h => new ApplicationHistoryEntry(h.ChangedDate, h.Comment))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");
    }

    public async Task SaveApplicantInfoAsync(
        Guid applicationId,
        SaveApplicantInfoDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        EnsureApplicantCanEditApplications();

        var applicantId = currentUser.Id;
        var application = await dbContext.RentalApplications
            .FirstOrDefaultAsync(
                app => app.RentalApplicationId == applicationId && app.ApplicantUserId == applicantId,
                cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");

        var applicantInfo = new ApplicantInfo(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            dto.PhoneNumber.Trim(),
            dto.Email.Trim(),
            dto.CurrentAddress.ToDomainAddress());

        application.SaveApplicantInfo(applicantInfo);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ResidenceListDto> GetApplicationResidencesAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        EnsureApplicantCanEditApplications();

        var applicantId = currentUser.Id;
        var result = await dbContext.RentalApplications
            .AsNoTracking()
            .Where(app =>
                app.RentalApplicationId == applicationId &&
                app.ApplicantUserId == applicantId)
            .Select(app => new
            {
                app.RentalApplicationId,
                app.Status,
                Residences = app.ResidenceHistories
                    .OrderByDescending(residence => residence.MoveInDate)
                    .Select(residence => new ResidenceDto(
                        residence.ResidenceHistoryId,
                        AddressDto.FromDomainAddress(residence.Address),
                        residence.LandlordName,
                        residence.LandlordPhone,
                        residence.MoveInDate,
                        residence.MoveOutDate))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");

        return new ResidenceListDto(
            result.RentalApplicationId,
            result.Residences,
            result.Status,
            ApplicationStatusRules.IsEditable(result.Status));
    }

    public async Task<ResidenceDto> GetResidenceAsync(
        Guid applicationId,
        Guid residenceHistoryId,
        CancellationToken cancellationToken = default)
    {
        EnsureApplicantCanEditApplications();

        var applicantId = currentUser.Id;
        var residence = await dbContext.RentalApplications
            .AsNoTracking()
            .Where(app =>
                app.RentalApplicationId == applicationId &&
                app.ApplicantUserId == applicantId)
            .SelectMany(app => app.ResidenceHistories)
            .Where(item => item.ResidenceHistoryId == residenceHistoryId)
            .Select(item => new ResidenceDto(
                item.ResidenceHistoryId,
                AddressDto.FromDomainAddress(item.Address),
                item.LandlordName,
                item.LandlordPhone,
                item.MoveInDate,
                item.MoveOutDate))
            .FirstOrDefaultAsync(cancellationToken);

        return residence
            ?? throw new NotFoundException($"Residence {residenceHistoryId} was not found.");
    }

    public async Task<Guid> AddResidenceAsync(
        Guid applicationId,
        SaveResidenceDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var application = await LoadOwnedApplicationAsync(applicationId, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var residence = application.AddResidence(
            dto.Address.ToDomainAddress(),
            dto.LandlordName,
            dto.LandlordPhone,
            dto.MoveInDate,
            dto.MoveOutDate,
            today);

        await dbContext.SaveChangesAsync(cancellationToken);
        return residence.ResidenceHistoryId;
    }

    public async Task UpdateResidenceAsync(
        Guid applicationId,
        Guid residenceHistoryId,
        SaveResidenceDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var application = await LoadOwnedApplicationAsync(applicationId, cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        application.UpdateResidence(
            residenceHistoryId,
            dto.Address.ToDomainAddress(),
            dto.LandlordName,
            dto.LandlordPhone,
            dto.MoveInDate,
            dto.MoveOutDate,
            today);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveResidenceAsync(
        Guid applicationId,
        Guid residenceHistoryId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadOwnedApplicationAsync(applicationId, cancellationToken);
        application.RemoveResidence(residenceHistoryId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureResidenceHistoryCompleteAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var application = await LoadOwnedApplicationAsync(applicationId, cancellationToken);
        application.EnsureResidenceHistoryComplete();
    }

    public async Task SubmitAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        EnsureApplicantCanEditApplications();

        var applicantId = currentUser.Id;
        var application = await dbContext.RentalApplications
            .Include(app => app.ResidenceHistories)
            .Include(app => app.Unit)
                .ThenInclude(unit => unit.Leases)
            .FirstOrDefaultAsync(
                app =>
                    app.RentalApplicationId == applicationId &&
                    app.ApplicantUserId == applicantId,
                cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        application.Submit(applicantId, now, today);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task WithdrawAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        EnsureApplicantCanEditApplications();

        var applicantId = currentUser.Id;
        var application = await dbContext.RentalApplications
            .FirstOrDefaultAsync(
                app =>
                    app.RentalApplicationId == applicationId &&
                    app.ApplicantUserId == applicantId,
                cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");

        var now = timeProvider.GetUtcNow();
        application.Withdraw(applicantId, now);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<RentalApplication> LoadOwnedApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        EnsureApplicantCanEditApplications();

        var applicantId = currentUser.Id;
        return await dbContext.RentalApplications
            .Include(app => app.ResidenceHistories)
            .FirstOrDefaultAsync(
                app =>
                    app.RentalApplicationId == applicationId &&
                    app.ApplicantUserId == applicantId,
                cancellationToken)
            ?? throw new NotFoundException($"Application {applicationId} was not found.");
    }

    private static List<ApplicationSection> BuildAllowedSections(
        bool isEditable,
        bool isApplicantInfoSaved,
        bool isResidenceHistorySaved)
    {
        if (!isEditable)
        {
            return
            [
                ApplicationSection.ApplicantInfo,
                ApplicationSection.ResidenceHistory,
                ApplicationSection.Summary
            ];
        }

        var allowed = new List<ApplicationSection> { ApplicationSection.ApplicantInfo };
        if (isApplicantInfoSaved)
        {
            allowed.Add(ApplicationSection.ResidenceHistory);
        }

        if (isApplicantInfoSaved && isResidenceHistorySaved)
        {
            allowed.Add(ApplicationSection.Summary);
        }

        return allowed;
    }

    /// <summary>
    /// Loads the application history for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of application status change entries.</returns>
    private async Task<IReadOnlyList<ApplicationStatusChangeDto>> LoadApplicationHistoryAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
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

    private void EnsureCanViewApplications()
    {
        if (!currentUser.IsPropertyManager && !currentUser.IsApplicant)
        {
            throw new ForbiddenException("You do not have permission to view rental applications.");
        }
    }

    private void EnsureApplicantCanEditApplications()
    {
        if (!currentUser.IsApplicant || currentUser.IsPropertyManager)
        {
            throw new ForbiddenException("Only applicants can edit their applications.");
        }
    }

    private static ApplicationPageState BuildApplicationPageState(
        ApplicationPageData pageData,
        bool isManager)
    {
        var isApplicantInfoSaved = pageData.ApplicantInfo is not null;
        var isResidenceHistorySaved = pageData.Residences.Count > 0;
        var incompleteSections = new List<ApplicationSection>();

        if (!isApplicantInfoSaved)
        {
            incompleteSections.Add(ApplicationSection.ApplicantInfo);
        }

        if (!isResidenceHistorySaved)
        {
            incompleteSections.Add(ApplicationSection.ResidenceHistory);
        }

        var isEditable = !isManager && ApplicationStatusRules.IsEditable(pageData.Status);
        return new ApplicationPageState(
            isApplicantInfoSaved,
            isResidenceHistorySaved,
            incompleteSections.Count == 0,
            incompleteSections,
            isEditable,
            BuildAllowedSections(isEditable, isApplicantInfoSaved, isResidenceHistorySaved));
    }

    private static ApplicationSection ResolveCurrentSection(
        ApplicationSection? requestedSection,
        ApplicationPageState state)
    {
        if (requestedSection.HasValue && state.AllowedSections.Contains(requestedSection.Value))
        {
            return requestedSection.Value;
        }

        return state.IsEditable
            ? ApplicationSection.ApplicantInfo
            : ApplicationSection.Summary;
    }

    /// <summary>
    /// Gets the most recent comment from the application history if one exists on the most recent change.
    /// </summary>
    /// <param name="history">The application history entries.</param>
    /// <returns>The most recent comment, or null if none exists.</returns>
    private static string? GetCurrentComment(IReadOnlyList<ApplicationHistoryEntry> history) =>
        history
            .OrderByDescending(entry => entry.ChangedDate)
            .Select(entry => entry.Comment)
            .FirstOrDefault();

    private sealed record ApplicationPageData(
        Guid ApplicationId,
        ApplicationStatus Status,
        UnitSummaryDto Unit,
        ApplicantInfoDto? ApplicantInfo,
        IReadOnlyList<ResidenceDto> Residences,
        IReadOnlyList<ApplicationHistoryEntry> HistoryEntries);

    private sealed record ApplicationHistoryEntry(DateTimeOffset ChangedDate, string? Comment);

    private sealed record ApplicationPageState(
        bool IsApplicantInfoSaved,
        bool IsResidenceHistorySaved,
        bool IsReadyToSubmit,
        IReadOnlyList<ApplicationSection> IncompleteSections,
        bool IsEditable,
        IReadOnlyList<ApplicationSection> AllowedSections);
}
