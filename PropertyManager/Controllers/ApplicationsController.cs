using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Applications;
using PropertyManager.Domain;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Controllers;

[Authorize]
public class ApplicationsController(IRentalApplicationService rentalApplicationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        ApplicationStatus? status,
        int? propertyId,
        CancellationToken cancellationToken)
    {
        var result = await rentalApplicationService.GetApplicationsAsync(
            new ApplicationListFilter(status, propertyId),
            cancellationToken);

        var model = ApplicationListViewModel.FromResult(result, status, propertyId);
        return View(model);
    }
}
