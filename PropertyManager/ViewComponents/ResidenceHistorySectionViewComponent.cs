using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Applications;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.ViewComponents;

public sealed class ResidenceHistorySectionViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        Guid applicationId,
        IReadOnlyList<ResidenceDto> residences,
        bool isEditable)
    {
        return View(new ResidenceHistorySectionViewModel(applicationId, residences, isEditable));
    }
}
