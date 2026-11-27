using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.Infrastructure.CurrentUser;
using PropertyManager.Models;

namespace PropertyManager.Controllers;

[AllowAnonymous]
public class HomeController(ICurrentUser currentUser) : Controller
{
    public IActionResult Index()
    {
        var isAuthenticated = currentUser.IsPropertyManager || currentUser.IsApplicant || User?.Identity?.IsAuthenticated == true;
        var model = new HomeIndexViewModel(
            isAuthenticated,
            currentUser.IsPropertyManager,
            currentUser.IsApplicant);

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
