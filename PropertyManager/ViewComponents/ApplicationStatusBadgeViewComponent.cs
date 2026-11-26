using Microsoft.AspNetCore.Mvc;
using PropertyManager.Domain;

namespace PropertyManager.ViewComponents;

public sealed class ApplicationStatusBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ApplicationStatus status)
    {
        var badgeClass = status switch
        {
            ApplicationStatus.Draft => "text-bg-secondary",
            ApplicationStatus.Submitted => "text-bg-primary",
            ApplicationStatus.Returned => "text-bg-warning",
            ApplicationStatus.Approved => "text-bg-success",
            ApplicationStatus.Denied => "text-bg-danger",
            ApplicationStatus.Withdrawn => "text-bg-dark",
            _ => "text-bg-secondary"
        };

        return View((status.ToString(), badgeClass));
    }
}
