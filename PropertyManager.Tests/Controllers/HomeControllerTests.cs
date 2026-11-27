using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertyManager.Controllers;
using PropertyManager.Infrastructure.CurrentUser;
using PropertyManager.Models;

namespace PropertyManager.Tests.Controllers;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsManagerFlags_WhenUserIsPropertyManager()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsPropertyManager).Returns(true);
        currentUser.Setup(u => u.IsApplicant).Returns(false);

        var controller = new HomeController(currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Index();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<HomeIndexViewModel>().Subject;
        model.IsAuthenticated.Should().BeTrue();
        model.IsPropertyManager.Should().BeTrue();
        model.IsApplicant.Should().BeFalse();
        model.Cards.Select(card => card.Title).Should().Equal("Properties & Units", "Rental Applications");
    }

    [Fact]
    public void Index_ReturnsApplicantFlags_WhenUserIsApplicant()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsPropertyManager).Returns(false);
        currentUser.Setup(u => u.IsApplicant).Returns(true);

        var controller = new HomeController(currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Index();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<HomeIndexViewModel>().Subject;
        model.IsAuthenticated.Should().BeTrue();
        model.IsPropertyManager.Should().BeFalse();
        model.IsApplicant.Should().BeTrue();
        model.Cards.Select(card => card.Title).Should().Equal("Available Units", "My Applications");
    }

    [Fact]
    public void Index_ReturnsUnauthenticatedFlags_WhenUserIsAnonymous()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsPropertyManager).Returns(false);
        currentUser.Setup(u => u.IsApplicant).Returns(false);

        var controller = new HomeController(currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Index();

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<HomeIndexViewModel>().Subject;
        model.IsAuthenticated.Should().BeFalse();
        model.IsPropertyManager.Should().BeFalse();
        model.IsApplicant.Should().BeFalse();
        model.Cards.Should().BeEmpty();
    }
}
