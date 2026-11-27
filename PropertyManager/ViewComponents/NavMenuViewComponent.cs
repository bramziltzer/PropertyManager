using Microsoft.AspNetCore.Mvc;
using PropertyManager.Infrastructure.CurrentUser;
using PropertyManager.Models;

namespace PropertyManager.ViewComponents;

public sealed class NavMenuViewComponent(ICurrentUser currentUser) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var activeController = ViewContext?.RouteData.Values["controller"]?.ToString();

        var items = new List<NavMenuItemViewModel>
        {
            CreateItem("Home", "Home", "Index", activeController)
        };

        if (currentUser.IsPropertyManager)
        {
            items.Add(CreateItem("Properties", "Properties", "Index", activeController));
        }

        if (currentUser.IsApplicant)
        {
            items.Add(CreateItem("Browse Units", "Units", "Index", activeController));
        }

        if (currentUser.IsPropertyManager || currentUser.IsApplicant)
        {
            items.Add(CreateItem("Applications", "Applications", "Index", activeController));
        }

        return View(items);
    }

    /// <summary>
    /// Creates a new navigation menu item view model.
    /// </summary>
    /// <param name="title">The display title of the menu item.</param>
    /// <param name="controller">The controller name for the menu item link, e.g., "Home".</param>
    /// <param name="action">The action name for the menu item link, e.g., "Index".</param>
    /// <param name="activeController">Used to determine if the item should be marked as active.</param>
    /// <returns>A new instance of <see cref="NavMenuItemViewModel"/>.</returns>
    private static NavMenuItemViewModel CreateItem(
        string title,
        string controller,
        string action,
        string? activeController)
    {
        var isActive = string.Equals(activeController, controller, StringComparison.OrdinalIgnoreCase);
        return new NavMenuItemViewModel(title, controller, action, isActive);
    }
}
