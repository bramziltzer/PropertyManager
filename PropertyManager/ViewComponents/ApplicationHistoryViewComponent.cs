using Microsoft.AspNetCore.Mvc;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.ViewComponents;

public sealed class ApplicationHistoryViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IReadOnlyList<ApplicationStatusChangeViewModel> history)
    {
        return View(history);
    }
}
