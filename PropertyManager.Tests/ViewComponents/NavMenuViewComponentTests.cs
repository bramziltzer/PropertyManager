using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Routing;
using Moq;
using PropertyManager.Infrastructure.CurrentUser;
using PropertyManager.Models;
using PropertyManager.ViewComponents;

namespace PropertyManager.Tests.ViewComponents;

public class NavMenuViewComponentTests
{
    [Fact]
    public void Invoke_ReturnsPropertiesAndApplications_ForPropertyManager()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsPropertyManager).Returns(true);
        currentUser.Setup(u => u.IsApplicant).Returns(false);

        var component = CreateComponent(currentUser.Object, "Properties");

        var result = component.Invoke();

        var viewResult = result.Should().BeOfType<ViewViewComponentResult>().Subject;
        var items = viewResult.ViewData!.Model.Should().BeAssignableTo<IReadOnlyList<NavMenuItemViewModel>>().Subject;
        items.Select(i => i.Controller).Should().Equal("Home", "Properties", "Applications");
        items.Single(i => i.Controller == "Properties").IsActive.Should().BeTrue();
    }

    [Fact]
    public void Invoke_ReturnsBrowseUnitsAndApplications_ForApplicant()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsPropertyManager).Returns(false);
        currentUser.Setup(u => u.IsApplicant).Returns(true);

        var component = CreateComponent(currentUser.Object, "Units");

        var result = component.Invoke();

        var viewResult = result.Should().BeOfType<ViewViewComponentResult>().Subject;
        var items = viewResult.ViewData!.Model.Should().BeAssignableTo<IReadOnlyList<NavMenuItemViewModel>>().Subject;
        items.Select(i => i.Controller).Should().Equal("Home", "Units", "Applications");
        items.Single(i => i.Controller == "Units").IsActive.Should().BeTrue();
    }

    [Fact]
    public void Invoke_ReturnsOnlyHome_ForAnonymousUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.IsPropertyManager).Returns(false);
        currentUser.Setup(u => u.IsApplicant).Returns(false);

        var component = CreateComponent(currentUser.Object, "Home");

        var result = component.Invoke();

        var viewResult = result.Should().BeOfType<ViewViewComponentResult>().Subject;
        var items = viewResult.ViewData!.Model.Should().BeAssignableTo<IReadOnlyList<NavMenuItemViewModel>>().Subject;
        items.Select(i => i.Controller).Should().Equal("Home");
        items.Single().IsActive.Should().BeTrue();
    }

    private static NavMenuViewComponent CreateComponent(ICurrentUser currentUser, string controller)
    {
        var routeData = new RouteData();
        routeData.Values["controller"] = controller;

        return new NavMenuViewComponent(currentUser)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { RouteData = routeData }
            }
        };
    }
}
