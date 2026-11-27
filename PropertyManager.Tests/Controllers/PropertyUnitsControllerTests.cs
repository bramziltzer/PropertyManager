using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using PropertyManager.AppService.Properties;
using PropertyManager.Controllers;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models;
using PropertyManager.Models.PropertyModels;

namespace PropertyManager.Tests.Controllers;

public class PropertyUnitsControllerTests
{
    private static readonly IReadOnlyList<UnitTypeOptionDto> ActiveOptions =
    [
        new(UnitTypeKind.Apartment, "Apartment", true),
        new(UnitTypeKind.House, "House", true)
    ];

    [Fact]
    public async Task CreatePost_ReturnsModalSuccessForPropertyUnitsRegion_WhenValid()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.CreateUnitAsync(5, It.IsAny<SaveUnitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        var controller = new PropertyUnitsController(service.Object);
        var model = new UnitFormViewModel
        {
            UnitNumber = "201",
            Bedrooms = 2,
            MonthlyRent = 1500m,
            Kind = UnitTypeKind.Apartment
        };

        var result = await controller.Create(5, model, CancellationToken.None);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        JsonSerializer.Serialize(json.Value).Should().Be("""{"success":true,"refresh":"property-units-5"}""");
    }

    [Fact]
    public async Task CreatePost_ReturnsModalFormWithFieldError_WhenDuplicateUnitNumberThrown()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.CreateUnitAsync(5, It.IsAny<SaveUnitRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateUnitNumberException("201"));
        service.Setup(s => s.GetUnitTypeOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveOptions);

        var controller = new PropertyUnitsController(service.Object);
        var model = new UnitFormViewModel
        {
            UnitNumber = "201",
            Bedrooms = 2,
            MonthlyRent = 1500m,
            Kind = UnitTypeKind.Apartment
        };

        var result = await controller.Create(5, model, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_UnitForm");
        controller.ModelState.Should().ContainKey(nameof(UnitFormViewModel.UnitNumber));
        model.UnitTypeOptions.Should().HaveCount(2);
    }

    [Fact]
    public async Task EditPost_ReturnsModalFormWithFieldError_WhenInactiveUnitTypeThrown()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.GetUnitAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnitDto(9, 5, "201", 2, 1500m, UnitTypeKind.Apartment, "Apartment", true, false));
        service.Setup(s => s.UpdateUnitAsync(5, 9, It.IsAny<SaveUnitRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InactiveUnitTypeException("Townhouse"));
        service.Setup(s => s.GetUnitTypeOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveOptions);

        var controller = new PropertyUnitsController(service.Object);
        var model = new UnitFormViewModel
        {
            UnitNumber = "201",
            Bedrooms = 2,
            MonthlyRent = 1500m,
            Kind = UnitTypeKind.Townhouse
        };

        var result = await controller.Edit(9, model, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_UnitForm");
        controller.ModelState.Should().ContainKey(nameof(UnitFormViewModel.Kind));
    }

    [Fact]
    public async Task DeletePost_ReturnsModalSuccessForPropertyUnitsRegion()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.GetUnitAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnitDto(9, 5, "201", 2, 1500m, UnitTypeKind.Apartment, "Apartment", true, false));
        service.Setup(s => s.DeleteUnitAsync(9, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new PropertyUnitsController(service.Object);

        var result = await controller.DeleteConfirmed(9, CancellationToken.None);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        JsonSerializer.Serialize(json.Value).Should().Be("""{"success":true,"refresh":"property-units-5"}""");
        service.Verify(s => s.DeleteUnitAsync(9, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGet_DisablesDelete_WhenUnitHasApplicationsOrLeases()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.GetUnitAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UnitDto(9, 5, "201", 2, 1500m, UnitTypeKind.Apartment, "Apartment", true, true));
        var controller = new PropertyUnitsController(service.Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/property-units/delete");
        controller.Url = url.Object;

        var result = await controller.Delete(9, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("Modal/_ConfirmDelete");
        var model = partial.Model.Should().BeOfType<ConfirmDeleteViewModel>().Subject;
        model.CanDelete.Should().BeFalse();
        model.Detail.Should().Be("This unit has applications or leases and cannot be deleted.");
    }
}
