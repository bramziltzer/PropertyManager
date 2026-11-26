using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PropertyManager.AppService;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Units;
using PropertyManager.Controllers;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models.UnitModels;

namespace PropertyManager.Tests.Controllers;

public class UnitsControllerTests
{
    [Fact]
    public async Task Index_PassesFilterAndPopulatesViewModel()
    {
        var unitService = new Mock<IRentalUnitService>();
        var appService = new Mock<IRentalApplicationService>(MockBehavior.Strict);
        var address = new AddressDto("1 Main St", null, "Madison", "WI", "53703");
        unitService
            .Setup(s => s.GetAvailableUnitsAsync(
                It.Is<AvailableUnitsFilter>(f => f.PropertyId == 3 && f.Bedrooms == 2 && f.MaxRent == 1800m),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AvailableUnitsResultDto(
                [new AvailableUnitDto(10, 3, "Oakridge", address, "101", 2, 1650m, "Apartment")],
                [new PropertyFilterOptionDto(3, "Oakridge")]));

        var controller = new UnitsController(unitService.Object, appService.Object);

        var result = await controller.Index(3, 2, 1800m, CancellationToken.None);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<AvailableUnitsListViewModel>().Subject;
        model.PropertyId.Should().Be(3);
        model.Bedrooms.Should().Be(2);
        model.MaxRent.Should().Be(1800m);
        model.Units.Should().ContainSingle(u => u.UnitNumber == "101");
        model.PropertyFilterOptions.Should().ContainSingle(o => o.Text == "Oakridge" && o.Selected);
    }

    [Fact]
    public async Task Start_RedirectsToApplicationEdit_WhenCreated()
    {
        var unitService = new Mock<IRentalUnitService>(MockBehavior.Strict);
        var appService = new Mock<IRentalApplicationService>();
        var applicationId = Guid.NewGuid();
        appService
            .Setup(s => s.StartAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicationId);

        var controller = new UnitsController(unitService.Object, appService.Object);

        var result = await controller.Start(10, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Edit");
        redirect.ControllerName.Should().Be("Applications");
        redirect.RouteValues!["id"].Should().Be(applicationId);
    }

    [Fact]
    public async Task Start_RedirectsToDetailsWithError_WhenDomainExceptionThrown()
    {
        var unitService = new Mock<IRentalUnitService>(MockBehavior.Strict);
        var appService = new Mock<IRentalApplicationService>();
        appService
            .Setup(s => s.StartAsync(10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateOpenApplicationException());

        var controller = new UnitsController(unitService.Object, appService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        var result = await controller.Start(10, CancellationToken.None);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(UnitsController.Details));
        redirect.RouteValues!["id"].Should().Be(10);
        controller.TempData["ErrorMessage"].Should().Be("You already have an open application for this unit.");
    }
}
