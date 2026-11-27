using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Infrastructure.CurrentUser;

namespace PropertyManager.Tests.AppService;

public class RentalApplicationServiceTests
{
    [Fact]
    public async Task StartAsync_CreatesDraftRecordingCurrentApplicant()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var applicationId = await service.StartApplicationAsync(unit.RentalUnitId);

        var saved = await dbContext.RentalApplications
            .AsNoTracking()
            .SingleAsync(a => a.RentalApplicationId == applicationId);
        saved.ApplicantUserId.Should().Be(TestData.ApplicantId);
        saved.RentalUnitId.Should().Be(unit.RentalUnitId);
        saved.Status.Should().Be(ApplicationStatus.Draft);
    }

    [Fact]
    public async Task StartAsync_RejectsNonApplicant_WithForbiddenException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ManagerId, isApplicant: false);

        await service.Invoking(s => s.StartApplicationAsync(unit.RentalUnitId))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task StartAsync_RejectsDuplicateOpenApplication()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.StartApplicationAsync(unit.RentalUnitId);

        await service.Invoking(s => s.StartApplicationAsync(unit.RentalUnitId))
            .Should().ThrowAsync<DuplicateOpenApplicationException>();
    }

    [Fact]
    public async Task StartAsync_AllowsNewApplication_WhenPreviousWasWithdrawn()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var previous = TestData.Draft(unit, TestData.ApplicantId);
        previous.Withdraw(TestData.ApplicantId, TestData.Now.AddDays(-1));
        dbContext.RentalApplications.Add(previous);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var newApplicationId = await service.StartApplicationAsync(unit.RentalUnitId);
        newApplicationId.Should().NotBe(previous.RentalApplicationId);
    }

    [Fact]
    public async Task StartAsync_RejectsUnitWithActiveLease()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser { Id = otherApplicantId, UserName = "other@example.com", Email = "other@example.com" });

        var approved = TestData.Submitted(unit, otherApplicantId);
        approved.Approve(TestData.ManagerId, TestData.Now.AddDays(-1), TestData.Today.AddDays(-1));
        dbContext.RentalApplications.Add(approved);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(s => s.StartApplicationAsync(unit.RentalUnitId))
            .Should().ThrowAsync<UnitNotAvailableException>();
    }

    [Fact]
    public async Task ListAsync_ForApplicant_ReturnsOnlyOwnedApplications()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var (unit1, unit2) = await SeedMultiPropertyApplicationsAsync(dbContext);
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var result = await service.GetApplicationsAsync(new ApplicationListFilter());

        result.IsPropertyManager.Should().BeFalse();
        result.Applications.Should().HaveCount(2);
        result.Applications.Should().OnlyContain(a =>
            a.RentalUnitId == unit1.RentalUnitId || a.RentalUnitId == unit2.RentalUnitId);
        result.Applications.Should().NotContain(a => a.ApplicantDisplayName == "other@example.com");
    }

    [Fact]
    public async Task ListAsync_ForManager_ReturnsAllApplications()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        await SeedMultiPropertyApplicationsAsync(dbContext);
        var service = CreateService(dbContext, TestData.ManagerId, isApplicant: false, isPropertyManager: true);

        var result = await service.GetApplicationsAsync(new ApplicationListFilter());

        result.IsPropertyManager.Should().BeTrue();
        result.Applications.Should().HaveCount(3);
        result.Applications.Should().Contain(a => a.ApplicantDisplayName == "other@example.com");
    }

    [Fact]
    public async Task ListAsync_AppliesStatusAndPropertyFilters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var (unit1, _) = await SeedMultiPropertyApplicationsAsync(dbContext);
        var service = CreateService(dbContext, TestData.ManagerId, isApplicant: false, isPropertyManager: true);

        var result = await service.GetApplicationsAsync(
            new ApplicationListFilter(ApplicationStatus.Submitted, unit1.PropertyId));

        result.Applications.Should().ContainSingle();
        var row = result.Applications[0];
        row.Status.Should().Be(ApplicationStatus.Submitted);
        row.PropertyId.Should().Be(unit1.PropertyId);
        row.ApplicantDisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task ListAsync_RejectsUserWithoutRole_WithForbiddenException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        await SeedUnitAsync(dbContext);
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: false, isPropertyManager: false);

        await service.Invoking(s => s.GetApplicationsAsync(new ApplicationListFilter()))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetApplicationPageAsync_ForOwner_ReturnsEditableDraftAndNoHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var draft = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(draft);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var page = await service.GetApplicationPageAsync(draft.RentalApplicationId);

        page.RentalApplicationId.Should().Be(draft.RentalApplicationId);
        page.Status.Should().Be(ApplicationStatus.Draft);
        page.CurrentSection.Should().Be(ApplicationSection.ApplicantInfo);
        page.IsEditable.Should().BeTrue();
        page.IsPropertyManager.Should().BeFalse();
        page.IncompleteSections.Should().Equal(
            ApplicationSection.ApplicantInfo,
            ApplicationSection.ResidenceHistory);
        page.AllowedSections.Should().ContainSingle().Which.Should().Be(ApplicationSection.ApplicantInfo);
        page.History.Should().BeNull();
    }

    [Fact]
    public async Task GetApplicationPageAsync_ForOtherApplicant_ThrowsNotFoundException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser { Id = otherApplicantId, UserName = "other@example.com", Email = "other@example.com" });
        var draft = TestData.Draft(unit, otherApplicantId);
        dbContext.RentalApplications.Add(draft);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(s => s.GetApplicationPageAsync(draft.RentalApplicationId))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetApplicationPageAsync_ForManager_ReturnsReadOnlyWithHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var submitted = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(submitted);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ManagerId, isApplicant: false, isPropertyManager: true);

        var page = await service.GetApplicationPageAsync(submitted.RentalApplicationId);

        page.IsEditable.Should().BeFalse();
        page.IsPropertyManager.Should().BeTrue();
        page.CurrentSection.Should().Be(ApplicationSection.Summary);
        page.History.Should().NotBeNull();
        page.History.Should().ContainSingle(h => h.NewStatus == ApplicationStatus.Submitted);
    }

    [Fact]
    public async Task SaveApplicantInfoAsync_PersistsInfoForOwner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var draft = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(draft);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);
        var dto = new SaveApplicantInfoDto(
            "Ada",
            "Lovelace",
            "555-0100",
            "ada@example.com",
            new PropertyManager.AppService.AddressDto("10 Main St", null, "Boston", "MA", "02101"));

        await service.SaveApplicantInfoAsync(draft.RentalApplicationId, dto);

        var saved = await dbContext.RentalApplications
            .AsNoTracking()
            .SingleAsync(a => a.RentalApplicationId == draft.RentalApplicationId);
        saved.ApplicantInfo.Should().NotBeNull();
        saved.ApplicantInfo!.FirstName.Should().Be("Ada");
        saved.ApplicantInfo.CurrentAddress.City.Should().Be("Boston");

        var page = await service.GetApplicationPageAsync(draft.RentalApplicationId);
        page.IncompleteSections.Should().Equal(ApplicationSection.ResidenceHistory);
    }

    [Fact]
    public async Task SaveApplicantInfoAsync_ForOtherApplicant_ThrowsNotFoundException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser { Id = otherApplicantId, UserName = "other@example.com", Email = "other@example.com" });
        var draft = TestData.Draft(unit, otherApplicantId);
        dbContext.RentalApplications.Add(draft);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);
        var dto = new SaveApplicantInfoDto(
            "Ada",
            "Lovelace",
            "555-0100",
            "ada@example.com",
            new PropertyManager.AppService.AddressDto("10 Main St", null, "Boston", "MA", "02101"));

        await service.Invoking(s => s.SaveApplicantInfoAsync(draft.RentalApplicationId, dto))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SaveApplicantInfoAsync_ForNonEditableApplication_ThrowsApplicationNotEditableException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var submitted = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(submitted);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);
        var dto = new SaveApplicantInfoDto(
            "Grace",
            "Hopper",
            "555-0200",
            "grace@example.com",
            new PropertyManager.AppService.AddressDto("20 Oak St", null, "Boston", "MA", "02102"));

        await service.Invoking(s => s.SaveApplicantInfoAsync(submitted.RentalApplicationId, dto))
            .Should().ThrowAsync<ApplicationNotEditableException>();
    }

    [Fact]
    public async Task ResidenceMutations_AddUpdateAndRemovePersist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);
        var residenceId = await service.AddResidenceAsync(
            application.RentalApplicationId,
            CreateResidenceDto("First Landlord"));

        var saved = await dbContext.RentalApplications
            .AsNoTracking()
            .Include(item => item.ResidenceHistories)
            .SingleAsync(item => item.RentalApplicationId == application.RentalApplicationId);
        saved.ResidenceHistories.Should().ContainSingle(item =>
            item.ResidenceHistoryId == residenceId &&
            item.LandlordName == "First Landlord");

        await service.UpdateResidenceAsync(
            application.RentalApplicationId,
            residenceId,
            CreateResidenceDto("Updated Landlord"));

        var updated = await service.GetResidenceAsync(
            application.RentalApplicationId,
            residenceId);
        updated.LandlordName.Should().Be("Updated Landlord");

        await service.RemoveResidenceAsync(application.RentalApplicationId, residenceId);

        var afterRemoval = await service.GetApplicationResidencesAsync(application.RentalApplicationId);
        afterRemoval.Residences.Should().BeEmpty();
        (await dbContext.ResidenceHistories.CountAsync()).Should().Be(0);
        dbContext.RentalApplications
            .AsNoTracking()
            .Single(item => item.RentalApplicationId == application.RentalApplicationId)
            .IsResidenceHistorySaved.Should().BeTrue();
    }

    [Fact]
    public async Task AddResidenceAsync_InvalidDates_DoesNotPersist()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);
        var request = new SaveResidenceDto(
            new PropertyManager.AppService.AddressDto("10 Main St", null, "Boston", "MA", "02101"),
            "Landlord",
            "555-0100",
            new DateOnly(2024, 6, 1),
            new DateOnly(2024, 5, 1));

        await service.Invoking(item => item.AddResidenceAsync(application.RentalApplicationId, request))
            .Should().ThrowAsync<InvalidResidenceDatesException>();

        (await dbContext.ResidenceHistories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ResidenceOperations_ForOtherApplicant_ReturnNotFound()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = otherApplicantId,
            UserName = "other@example.com",
            Email = "other@example.com"
        });
        var application = TestData.Draft(unit, otherApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(item => item.GetApplicationResidencesAsync(application.RentalApplicationId))
            .Should().ThrowAsync<NotFoundException>();
        await service.Invoking(item => item.AddResidenceAsync(
                application.RentalApplicationId,
                CreateResidenceDto("Landlord")))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ResidenceMutations_AfterSubmissionAreRejected()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();
        var residenceId = application.ResidenceHistories.Single().ResidenceHistoryId;
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(item => item.AddResidenceAsync(
                application.RentalApplicationId,
                CreateResidenceDto("Landlord")))
            .Should().ThrowAsync<ApplicationNotEditableException>();
        await service.Invoking(item => item.UpdateResidenceAsync(
                application.RentalApplicationId,
                residenceId,
                CreateResidenceDto("Changed")))
            .Should().ThrowAsync<ApplicationNotEditableException>();
        await service.Invoking(item => item.RemoveResidenceAsync(
                application.RentalApplicationId,
                residenceId))
            .Should().ThrowAsync<ApplicationNotEditableException>();
        await service.Invoking(item => item.SaveResidenceHistoryAsync(application.RentalApplicationId))
            .Should().ThrowAsync<ApplicationNotEditableException>();
    }

    [Fact]
    public async Task SaveResidenceHistoryAsync_AllowsAndPersistsEmptyResidenceHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        var savedPage = await service.GetApplicationPageAsync(application.RentalApplicationId);
        savedPage.IsResidenceHistorySaved.Should().BeFalse();

        await service.Invoking(item =>
                item.SaveResidenceHistoryAsync(application.RentalApplicationId))
            .Should().NotThrowAsync();

        savedPage = await service.GetApplicationPageAsync(application.RentalApplicationId);
        savedPage.IsResidenceHistorySaved.Should().BeTrue();
        savedPage.Residences.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitAsync_MissingRequiredSection_ThrowsApplicationIncompleteException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Draft(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(item => item.SubmitAsync(application.RentalApplicationId))
            .Should().ThrowAsync<ApplicationIncompleteException>()
            .WithMessage("*Applicant Information*Residence History*");
    }

    [Fact]
    public async Task SubmitAsync_UnitWithActiveLease_ThrowsUnitNotAvailableException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = otherApplicantId,
            UserName = "other@example.com",
            Email = "other@example.com"
        });

        var application = TestData.Completed(unit, TestData.ApplicantId);
        var competing = TestData.Submitted(unit, otherApplicantId);
        competing.Approve(TestData.ManagerId, TestData.Now.AddDays(-1), TestData.Today.AddDays(-1));
        dbContext.RentalApplications.AddRange(application, competing);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(item => item.SubmitAsync(application.RentalApplicationId))
            .Should().ThrowAsync<UnitNotAvailableException>();
    }

    [Fact]
    public async Task SubmitAsync_ValidDraft_SubmitsAndAddsExpectedHistoryEvent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Completed(unit, TestData.ApplicantId);
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.SubmitAsync(application.RentalApplicationId);

        var saved = await dbContext.RentalApplications
            .AsNoTracking()
            .Include(a => a.History)
            .SingleAsync(a => a.RentalApplicationId == application.RentalApplicationId);
        saved.Status.Should().Be(ApplicationStatus.Submitted);
        var change = saved.History.Should().ContainSingle().Subject;
        change.OldStatus.Should().Be(ApplicationStatus.Draft);
        change.NewStatus.Should().Be(ApplicationStatus.Submitted);
        change.ChangedByUserId.Should().Be(TestData.ApplicantId);
        change.ChangedDate.Should().Be(TestData.Now);
        change.Comment.Should().BeNull();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Returned)]
    [InlineData(ApplicationStatus.Submitted)]
    public async Task WithdrawAsync_ValidSourceStatuses_WithdrawsApplication(ApplicationStatus sourceStatus)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = sourceStatus switch
        {
            ApplicationStatus.Draft => TestData.Draft(unit, TestData.ApplicantId),
            ApplicationStatus.Submitted => TestData.Submitted(unit, TestData.ApplicantId),
            ApplicationStatus.Returned => CreateReturnedApplication(unit),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceStatus))
        };
        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.WithdrawAsync(application.RentalApplicationId);

        var saved = await dbContext.RentalApplications
            .AsNoTracking()
            .Include(a => a.History)
            .SingleAsync(a => a.RentalApplicationId == application.RentalApplicationId);
        saved.Status.Should().Be(ApplicationStatus.Withdrawn);
        saved.History.Should().Contain(h =>
            h.OldStatus == sourceStatus
            && h.NewStatus == ApplicationStatus.Withdrawn
            && h.ChangedByUserId == TestData.ApplicantId
            && h.ChangedDate == TestData.Now);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public async Task WithdrawAsync_TerminalStatus_ThrowsInvalidStatusTransitionException(ApplicationStatus terminalStatus)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var unit = await SeedUnitAsync(dbContext);
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        switch (terminalStatus)
        {
            case ApplicationStatus.Approved:
                application.Approve(TestData.ManagerId, TestData.Now.AddDays(-1), TestData.Today.AddDays(-1));
                break;
            case ApplicationStatus.Denied:
                application.Deny(TestData.ManagerId, TestData.Now.AddDays(-1), "Denied");
                break;
            case ApplicationStatus.Withdrawn:
                application.Withdraw(TestData.ApplicantId, TestData.Now.AddDays(-1));
                break;
        }

        dbContext.RentalApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TestData.ApplicantId, isApplicant: true);

        await service.Invoking(item => item.WithdrawAsync(application.RentalApplicationId))
            .Should().ThrowAsync<InvalidStatusTransitionException>();
    }

    private static RentalApplication CreateReturnedApplication(RentalUnit unit)
    {
        var application = TestData.Submitted(unit, TestData.ApplicantId);
        application.Return(TestData.ManagerId, TestData.Now.AddDays(-1), "Please update.");
        return application;
    }

    private static SaveResidenceDto CreateResidenceDto(string landlordName) => new(
        new PropertyManager.AppService.AddressDto("10 Main St", null, "Boston", "MA", "02101"),
        landlordName,
        "555-0100",
        new DateOnly(2020, 1, 1),
        null);

    private static async Task<(RentalUnit Unit1, RentalUnit Unit2)> SeedMultiPropertyApplicationsAsync(
        ApplicationDbContext dbContext)
    {
        var unit1 = await SeedUnitAsync(dbContext);

        var otherApplicantId = Guid.NewGuid();
        dbContext.Users.Add(new ApplicationUser
        {
            Id = otherApplicantId,
            UserName = "other@example.com",
            Email = "other@example.com"
        });

        var apartment = await dbContext.UnitTypes.FirstAsync();
        var property2 = new Property("Maplewood", TestData.Address());
        var unit2 = new RentalUnit("202", 1, 1250m, apartment);
        property2.AddUnit(unit2);
        dbContext.Properties.Add(property2);
        await dbContext.SaveChangesAsync();

        var ownSubmitted = TestData.Submitted(unit1, TestData.ApplicantId);
        var ownDraft = TestData.Draft(unit2, TestData.ApplicantId);
        var otherDraft = TestData.Draft(unit1, otherApplicantId);

        dbContext.RentalApplications.AddRange(ownSubmitted, ownDraft, otherDraft);
        await dbContext.SaveChangesAsync();

        return (unit1, unit2);
    }

    private static async Task<RentalUnit> SeedUnitAsync(ApplicationDbContext dbContext)
    {
        var apartment = TestData.ActiveType(UnitTypeKind.Apartment);
        dbContext.UnitTypes.Add(apartment);
        dbContext.ApplicationStatusTypes.AddRange(
            new ApplicationStatusType { Status = ApplicationStatus.Draft, Name = "Draft" },
            new ApplicationStatusType { Status = ApplicationStatus.Submitted, Name = "Submitted" },
            new ApplicationStatusType { Status = ApplicationStatus.Returned, Name = "Returned" },
            new ApplicationStatusType { Status = ApplicationStatus.Approved, Name = "Approved" },
            new ApplicationStatusType { Status = ApplicationStatus.Denied, Name = "Denied" },
            new ApplicationStatusType { Status = ApplicationStatus.Withdrawn, Name = "Withdrawn" });
        dbContext.Users.AddRange(
            new ApplicationUser
            {
                Id = TestData.ApplicantId,
                UserName = "applicant@example.com",
                Email = "applicant@example.com"
            },
            new ApplicationUser
            {
                Id = TestData.ManagerId,
                UserName = "manager@example.com",
                Email = "manager@example.com"
            });

        var property = new Property("Oakridge", TestData.Address());
        var unit = new RentalUnit("101", 2, 1650m, apartment);
        property.AddUnit(unit);
        dbContext.Properties.Add(property);
        await dbContext.SaveChangesAsync();

        return unit;
    }

    private static RentalApplicationService CreateService(
        ApplicationDbContext dbContext,
        Guid userId,
        bool isApplicant,
        bool? isPropertyManager = null) =>
        new(
            dbContext,
            new StubCurrentUser(userId, isApplicant, isPropertyManager ?? !isApplicant),
            new FixedTimeProvider(TestData.Now));

    private static async Task<ApplicationDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private sealed class StubCurrentUser(Guid id, bool isApplicant, bool isPropertyManager) : ICurrentUser
    {
        public Guid Id => id;
        public bool IsApplicant => isApplicant;
        public bool IsPropertyManager => isPropertyManager;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
