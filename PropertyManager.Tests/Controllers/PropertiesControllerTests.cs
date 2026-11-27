using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using PropertyManager.AppService;
using PropertyManager.AppService.Properties;
using PropertyManager.Controllers;
using PropertyManager.Models;
using PropertyManager.Models.PropertyModels;

namespace PropertyManager.Tests.Controllers;

public class PropertiesControllerTests
{
    [Theory]
    [InlineData("53703", true)]
    [InlineData("53703-1234", true)]
    [InlineData("1234", false)]
    [InlineData("53703-12", false)]
    [InlineData("abcde", false)]
    public void PropertyFormViewModel_ValidatesZipCodeFormat(string zipCode, bool expectedValid)
    {
        var model = new PropertyFormViewModel
        {
            Name = "Oakwood",
            Line1 = "1 Main St",
            City = "Madison",
            State = "WI",
            ZipCode = zipCode
        };

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        isValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            results.Should().Contain(r => r.MemberNames.Contains(nameof(PropertyFormViewModel.ZipCode)));
        }
    }

    [Fact]
    public async Task CreatePost_ReturnsModalForm_WhenModelStateIsInvalid()
    {
        var service = new Mock<IPropertyService>(MockBehavior.Strict);
        var controller = new PropertiesController(service.Object);
        controller.ModelState.AddModelError(nameof(PropertyFormViewModel.Name), "Required");
        var model = new PropertyFormViewModel();

        var result = await controller.Create(model, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_PropertyForm");
        partial.Model.Should().BeSameAs(model);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreatePost_ReturnsModalSuccess_WhenValid()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.CreateAsync(It.IsAny<SavePropertyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var controller = new PropertiesController(service.Object);
        var model = new PropertyFormViewModel
        {
            Name = "Oakwood",
            Line1 = "1 Main St",
            City = "Madison",
            State = "WI",
            ZipCode = "53703"
        };

        var result = await controller.Create(model, CancellationToken.None);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        JsonSerializer.Serialize(json.Value).Should().Be("""{"success":true,"refresh":"properties"}""");
        service.Verify(s => s.CreateAsync(
            It.Is<SavePropertyRequest>(r => r.Name == "Oakwood" && r.Address.Line1 == "1 Main St"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EditPost_SetsPropertyIdAndReturnsModalSuccess_WhenValid()
    {
        var service = new Mock<IPropertyService>();
        var controller = new PropertiesController(service.Object);
        var model = new PropertyFormViewModel
        {
            Name = "Oakwood Updated",
            Line1 = "2 Main St",
            City = "Madison",
            State = "WI",
            ZipCode = "53703"
        };

        var result = await controller.Edit(7, model, CancellationToken.None);

        model.PropertyId.Should().Be(7);
        var json = result.Should().BeOfType<JsonResult>().Subject;
        JsonSerializer.Serialize(json.Value).Should().Be("""{"success":true,"refresh":"properties"}""");
        service.Verify(s => s.UpdateAsync(7, It.IsAny<SavePropertyRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGet_DisablesDelete_WhenPropertyHasUnits()
    {
        var service = new Mock<IPropertyService>();
        service.Setup(s => s.GetPropertyAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetailDto(
                7,
                "Oakwood",
                new AddressDto("1 Main St", null, "Madison", "WI", "53703"),
                2));
        var controller = new PropertiesController(service.Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/properties/delete");
        controller.Url = url.Object;

        var result = await controller.Delete(7, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("Modal/_ConfirmDelete");
        var model = partial.Model.Should().BeOfType<ConfirmDeleteViewModel>().Subject;
        model.CanDelete.Should().BeFalse();
        model.Detail.Should().Contain("2 unit(s)");
    }
}
