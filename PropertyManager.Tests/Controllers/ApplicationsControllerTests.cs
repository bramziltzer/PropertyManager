using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertyManager.AppService;
using PropertyManager.AppService.Applications;
using PropertyManager.Controllers;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Controllers;

public class ApplicationsControllerTests
{
    [Fact]
    public async Task Index_PassesFilterAndPopulatesViewModel()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();

        apList      .Setup(s => s.GetApplicationsAsync(
                It.Is<ApplicationListFilter>(f => f.Status == ApplicationStatus.Submitted && f.PropertyId == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationListResultDto(
                [
                    new ApplicationListItemDto(
                        applicationId,
                        3,
                        "Oakridge",
                        10,
                        "101",
                        1650m,
                        ApplicationStatus.Submitted,
                        "Jane Doe",
                        false)
                ],
                [new PropertyFilterOptionDto(3, "Oakridge")],
                IsPropertyManager: true));

        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Index(ApplicationStatus.Submitted, 3, CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ApplicationListViewModel>().Subject;
        model.IsPropertyManager.Should().BeTrue();
        model.Filter.Status.Should().Be(ApplicationStatus.Submitted);
        model.Filter.PropertyId.Should().Be(3);
        model.Filter.StatusOptions.Should().Contain(o => o.Value == "Submitted" && o.Selected);
        model.Filter.PropertyOptions.Should().ContainSingle(o => o.Text == "Oakridge" && o.Selected);
        model.Applications.Should().ContainSingle(a =>
            a.RentalApplicationId == applicationId
            && a.ApplicantDisplayName == "Jane Doe"
            && a.Status == ApplicationStatus.Submitted);
    }

    [Fact]
    public async Task EditGet_ReturnsViewWithPageModel()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService
            .Setup(s => s.GetApplicationPageAsync(applicationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageDto(applicationId, isEditable: true));

        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(applicationId, null, CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ApplicationPageViewModel>().Subject;
        model.RentalApplicationId.Should().Be(applicationId);
        model.IsEditable.Should().BeTrue();
        model.ApplicantInfo.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public async Task EditPost_ValidContinue_SavesApplicantInfoAndRedirectsToResidenceHistory()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        var controller = new ApplicationsController(appService.Object);
        var model = new ApplicationPageViewModel
        {
            ApplicantInfo = new ApplicantInfoSectionViewModel
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                PhoneNumber = "555-0100",
                Email = "ada@example.com",
                CurrentAddress = new AddressViewModel
                {
                    Line1 = "10 Main St",
                    City = "Boston",
                    State = "MA",
                    ZipCode = "02101"
                }
            }
        };

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.ApplicantInfo,
            "continue",
            model,
            CancellationToken.None);

        appService.Verify(s => s.SaveApplicantInfoAsync(
                applicationId,
                It.Is<SaveApplicantInfoDto>(d => d.FirstName == "Ada" && d.CurrentAddress.ZipCode == "02101"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(ApplicationsController.Edit));
        redirect.RouteValues!["id"].Should().Be(applicationId);
        redirect.RouteValues["section"].Should().Be(ApplicationSection.ResidenceHistory);
    }

    [Fact]
    public async Task EditPost_InvalidContinue_DoesNotPersistAndRerendersApplicantInfo()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService
            .Setup(s => s.GetApplicationPageAsync(applicationId, ApplicationSection.ApplicantInfo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageDto(applicationId, isEditable: true));

        var controller = new ApplicationsController(appService.Object);
        controller.ModelState.AddModelError("ApplicantInfo.FirstName", "Required");

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.ApplicantInfo,
            "continue",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        appService.Verify(s => s.SaveApplicantInfoAsync(
                It.IsAny<Guid>(),
                It.IsAny<SaveApplicantInfoDto>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ApplicationPageViewModel>().Subject;
        model.CurrentSection.Should().Be(ApplicationSection.ApplicantInfo);
        model.ApplicantInfo.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public async Task EditPost_InvalidContinueOnNonEditableApplication_ThrowsApplicationNotEditableException()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService
            .Setup(s => s.GetApplicationPageAsync(applicationId, ApplicationSection.ApplicantInfo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageDto(applicationId, isEditable: false, status: ApplicationStatus.Submitted));

        var controller = new ApplicationsController(appService.Object);
        controller.ModelState.AddModelError("ApplicantInfo.FirstName", "Required");

        await controller.Invoking(c => c.Edit(
                applicationId,
                ApplicationSection.ApplicantInfo,
                "continue",
                new ApplicationPageViewModel(),
                CancellationToken.None))
            .Should().ThrowAsync<ApplicationNotEditableException>();
    }

    [Fact]
    public async Task EditPost_ResidenceContinue_EnsuresCompleteAndRedirectsToSummary()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.ResidenceHistory,
            "continue",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        appService.Verify(item => item.SaveResidenceHistoryAsync(
            applicationId,
            It.IsAny<CancellationToken>()), Times.Once);
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.RouteValues!["section"].Should().Be(ApplicationSection.Summary);
    }

    [Fact]
    public async Task EditPost_ResidenceBack_DoesNotPersistAndRedirectsToApplicantInfo()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.ResidenceHistory,
            "back",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        appService.VerifyNoOtherCalls();
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.RouteValues!["section"].Should().Be(ApplicationSection.ApplicantInfo);
    }

    [Fact]
    public async Task EditPost_ResidenceContinueWithoutResidence_RedirectsToSummary()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService.Setup(item => item.SaveResidenceHistoryAsync(
                applicationId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.ResidenceHistory,
            "continue",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.RouteValues["section"].Should().Be(ApplicationSection.Summary);
    }

    [Fact]
    public async Task EditPost_SummarySubmit_SubmitsAndRedirectsToSummary()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.Summary,
            "submit",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        appService.Verify(item => item.SubmitAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(ApplicationsController.Edit));
        redirect.RouteValues!["id"].Should().Be(applicationId);
        redirect.RouteValues["section"].Should().Be(ApplicationSection.Summary);
    }

    [Fact]
    public async Task EditPost_SummarySubmit_WhenUnitUnavailable_RerendersSummaryWithError()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService.Setup(item => item.SubmitAsync(applicationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnitNotAvailableException(10));
        appService.Setup(item => item.GetApplicationPageAsync(
                applicationId,
                ApplicationSection.Summary,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageDto(
                applicationId,
                isEditable: true,
                currentSection: ApplicationSection.Summary));
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.Summary,
            "submit",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task EditPost_SummaryBack_RedirectsToResidenceHistoryWithoutSubmitting()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Edit(
            applicationId,
            ApplicationSection.Summary,
            "back",
            new ApplicationPageViewModel(),
            CancellationToken.None);

        appService.VerifyNoOtherCalls();
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.RouteValues!["section"].Should().Be(ApplicationSection.ResidenceHistory);
    }

    [Fact]
    public async Task WithdrawGet_ValidStatus_ReturnsWithdrawConfirmPartial()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService.Setup(item => item.GetApplicationPageAsync(applicationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageDto(applicationId, isEditable: false, status: ApplicationStatus.Submitted));
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.Withdraw(applicationId, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_WithdrawConfirm");
        var model = partial.Model.Should().BeOfType<WithdrawConfirmViewModel>().Subject;
        model.RentalApplicationId.Should().Be(applicationId);
        model.PropertyName.Should().Be("Oakridge");
        model.UnitNumber.Should().Be("101");
    }

    [Fact]
    public async Task WithdrawGet_TerminalStatus_ThrowsInvalidStatusTransitionException()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService.Setup(item => item.GetApplicationPageAsync(applicationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageDto(applicationId, isEditable: false, status: ApplicationStatus.Approved));
        var controller = new ApplicationsController(appService.Object);

        await controller.Invoking(c => c.Withdraw(applicationId, CancellationToken.None))
            .Should().ThrowAsync<InvalidStatusTransitionException>();
    }

    [Fact]
    public async Task WithdrawPost_CallsWithdrawAndRedirectsToSummary()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        var controller = new ApplicationsController(appService.Object);

        var result = await controller.WithdrawConfirmed(applicationId, CancellationToken.None);

        appService.Verify(item => item.WithdrawAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(ApplicationsController.Edit));
        redirect.RouteValues!["id"].Should().Be(applicationId);
        redirect.RouteValues["section"].Should().Be(ApplicationSection.Summary);
    }

    private static ApplicationPageDto CreatePageDto(
        Guid applicationId,
        bool isEditable,
        ApplicationStatus status = ApplicationStatus.Draft,
        ApplicationSection currentSection = ApplicationSection.ApplicantInfo) => new(
        applicationId,
        status,
        currentSection,
        new UnitSummaryDto(
            10,
            3,
            "Oakridge",
            new AddressDto("100 Oak St", null, "Boston", "MA", "02101"),
            "101",
            2,
            1650m,
            "Apartment"),
        null,
        [],
        false,
        false,
        false,
        [ApplicationSection.ApplicantInfo, ApplicationSection.ResidenceHistory],
        isEditable,
        false,
        [currentSection],
        null,
        null);
}
