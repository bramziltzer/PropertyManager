using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Properties;
using PropertyManager.Models.PropertyModels;

namespace PropertyManager.ViewComponents;

public sealed class PropertyUnitsViewComponent(IPropertyService propertyService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        int propertyId,
        IReadOnlyList<UnitListItemViewModel>? units = null,
        CancellationToken cancellationToken = default)
    {
        var unitItems = units ?? (await propertyService.GetUnitsAsync(propertyId, cancellationToken))
            .Select(UnitListItemViewModel.FromDto)
            .ToList();

        return View(new PropertyUnitsViewModel(propertyId, unitItems));
    }
}
