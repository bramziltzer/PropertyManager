using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Controllers;

namespace PropertyManager.Tests.Infrastructure;

public class ModalResultsTests
{
    [Fact]
    public void ModalSuccess_ReturnsRefreshContract()
    {
        var controller = new TestAppController();

        var result = controller.Success("properties");

        result.Value.Should().NotBeNull();
        JsonSerializer.Serialize(result.Value).Should().Be("""{"success":true,"refresh":"properties"}""");
    }

    [Fact]
    public void ModalForm_ReturnsRequestedPartialAndModel()
    {
        var controller = new TestAppController();
        var model = new object();

        var result = controller.Form("_PropertyForm", model);

        result.ViewName.Should().Be("_PropertyForm");
        result.Model.Should().BeSameAs(model);
    }

    private sealed class TestAppController : AppController
    {
        public JsonResult Success(string refreshTarget) => ModalSuccess(refreshTarget);

        public PartialViewResult Form(string partialName, object model) => ModalForm(partialName, model);
    }
}
