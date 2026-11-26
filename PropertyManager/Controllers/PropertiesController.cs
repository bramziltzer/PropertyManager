using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Properties;
using PropertyManager.Domain;
using PropertyManager.Models;
using PropertyManager.Models.PropertyModels;

namespace PropertyManager.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class PropertiesController(IPropertyService propertyService) : Controller
{
    private const string RefreshRegion = "properties";

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var properties = await LoadPropertyList(cancellationToken);
        return View(properties);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var properties = await LoadPropertyList(cancellationToken);
        return PartialView("_PropertyList", properties);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return this.ModalForm("_PropertyForm", new PropertyFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return this.ModalForm("_PropertyForm", model);
        }

        await propertyService.CreateAsync(model.ToRequest(), cancellationToken);
        return this.ModalSuccess(RefreshRegion);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int propertyId, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPropertyAsync(propertyId, cancellationToken);
        return this.ModalForm("_PropertyForm", PropertyFormViewModel.FromDto(property));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int propertyId, PropertyFormViewModel model, CancellationToken cancellationToken)
    {
        model.PropertyId = propertyId;
        if (!ModelState.IsValid)
        {
            return this.ModalForm("_PropertyForm", model);
        }

        await propertyService.UpdateAsync(propertyId, model.ToRequest(), cancellationToken);
        return this.ModalSuccess(RefreshRegion);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int propertyId, CancellationToken cancellationToken)
    {
        var property = await propertyService.GetPropertyAsync(propertyId, cancellationToken);
        var model = new ConfirmDeleteViewModel
        {
            ActionUrl = Url.Action(nameof(Delete), new { propertyId })!,
            EntityName = property.Name,
            Detail = property.UnitCount > 0
                ? $"This property currently has {property.UnitCount} unit(s) and cannot be deleted until they are removed."
                : null
        };

        return this.ModalForm("Modal/_ConfirmDelete", model);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int propertyId, CancellationToken cancellationToken)
    {
        await propertyService.DeleteAsync(propertyId, cancellationToken);
        return this.ModalSuccess(RefreshRegion);
    }

    private async Task<IReadOnlyList<PropertyListItemViewModel>> LoadPropertyList(CancellationToken cancellationToken)
    {
        var properties = await propertyService.GetPropertiesAsync(cancellationToken);
        return properties.Select(PropertyListItemViewModel.FromDto).ToList();
    }
}
