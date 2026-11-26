using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Properties;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models;
using PropertyManager.Models.PropertyModels;

namespace PropertyManager.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class PropertyUnitsController(IPropertyService propertyService) : Controller
{
    private static string UnitsRefreshRegion(int propertyId) => $"property-units-{propertyId}";

    [HttpGet]
    public IActionResult List(int propertyId)
    {
        return ViewComponent("PropertyUnits", new { propertyId });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId, CancellationToken cancellationToken)
    {
        await propertyService.GetPropertyAsync(propertyId, cancellationToken);
        var options = await propertyService.GetUnitTypeOptionsAsync(cancellationToken);
        var model = new UnitFormViewModel { PropertyId = propertyId }.WithOptions(options);
        return this.ModalForm("_UnitForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int propertyId, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        model.PropertyId = propertyId;
        if (!ModelState.IsValid)
        {
            var options = await propertyService.GetUnitTypeOptionsAsync(cancellationToken);
            return this.ModalForm("_UnitForm", model.WithOptions(options));
        }

        try
        {
            await propertyService.CreateUnitAsync(propertyId, model.ToRequest(), cancellationToken);
            return this.ModalSuccess(UnitsRefreshRegion(propertyId));
        }
        catch (DuplicateUnitNumberException ex)
        {
            ModelState.AddModelError(nameof(UnitFormViewModel.UnitNumber), ex.Message);
        }
        catch (InactiveUnitTypeException ex)
        {
            ModelState.AddModelError(nameof(UnitFormViewModel.Kind), ex.Message);
        }

        var unitTypes = await propertyService.GetUnitTypeOptionsAsync(cancellationToken);
        return this.ModalForm("_UnitForm", model.WithOptions(unitTypes));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var unit = await propertyService.GetUnitAsync(id, cancellationToken);
        var options = await propertyService.GetUnitTypeOptionsAsync(cancellationToken);
        return this.ModalForm("_UnitForm", UnitFormViewModel.FromDto(unit, options));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UnitFormViewModel model, CancellationToken cancellationToken)
    {
        var existing = await propertyService.GetUnitAsync(id, cancellationToken);
        model.RentalUnitId = id;
        model.PropertyId = existing.PropertyId;

        if (!ModelState.IsValid)
        {
            var options = await propertyService.GetUnitTypeOptionsAsync(cancellationToken);
            return this.ModalForm("_UnitForm", model.WithOptions(options, existing.Kind));
        }

        try
        {
            await propertyService.UpdateUnitAsync(existing.PropertyId, id, model.ToRequest(), cancellationToken);
            return this.ModalSuccess(UnitsRefreshRegion(existing.PropertyId));
        }
        catch (DuplicateUnitNumberException ex)
        {
            ModelState.AddModelError(nameof(UnitFormViewModel.UnitNumber), ex.Message);
        }
        catch (InactiveUnitTypeException ex)
        {
            ModelState.AddModelError(nameof(UnitFormViewModel.Kind), ex.Message);
        }

        var unitTypes = await propertyService.GetUnitTypeOptionsAsync(cancellationToken);
        return this.ModalForm("_UnitForm", model.WithOptions(unitTypes, existing.Kind));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var unit = await propertyService.GetUnitAsync(id, cancellationToken);
        var model = new ConfirmDeleteViewModel
        {
            ActionUrl = Url.Action(nameof(Delete), new { id })!,
            EntityName = $"Unit {unit.UnitNumber}",
            Detail = unit.HasApplicationsOrLeases
                ? "This unit has applications or leases and cannot be deleted."
                : null
        };

        return this.ModalForm("Modal/_ConfirmDelete", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var unit = await propertyService.GetUnitAsync(id, cancellationToken);
        await propertyService.DeleteUnitAsync(id, cancellationToken);
        return this.ModalSuccess(UnitsRefreshRegion(unit.PropertyId));
    }
}
