using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Units;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models.UnitModels;

namespace PropertyManager.Controllers;

[Authorize(Roles = RoleNames.Applicant)]
public class UnitsController(
    IRentalUnitService rentalUnitService,
    IRentalApplicationService rentalApplicationService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int? propertyId,
        int? bedrooms,
        decimal? maxRent,
        CancellationToken cancellationToken)
    {
        var filter = new AvailableUnitsFilter(propertyId, bedrooms, maxRent);
        var result = await rentalUnitService.GetAvailableUnitsAsync(filter, cancellationToken);
        var model = AvailableUnitsListViewModel.FromResult(result, propertyId, bedrooms, maxRent);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var dto = await rentalUnitService.GetUnitDetailsAsync(id, cancellationToken);
        return View(UnitDetailsViewModel.FromDto(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Start(int unitId, CancellationToken cancellationToken)
    {
        try
        {
            var applicationId = await rentalApplicationService.StartAsync(unitId, cancellationToken);
            return RedirectToAction("Edit", "Applications", new { id = applicationId });
        }
        catch (DomainException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id = unitId });
        }
    }
}
