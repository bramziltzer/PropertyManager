using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertyManager.AppService;
using PropertyManager.AppService.Applications;
using PropertyManager.Controllers;
using PropertyManager.Domain;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Controllers;

public class ApplicationsControllerTests
{
    [Fact]
    public async Task Index_PassesFilterAndPopulatesViewModel()
    {
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();

        appService
            .Setup(s => s.GetApplicationsAsync(
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
}
