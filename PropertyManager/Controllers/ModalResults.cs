using Microsoft.AspNetCore.Mvc;

namespace PropertyManager.Controllers;

public static class ModalResults
{
    public static JsonResult ModalSuccess(this Controller controller, string refreshTarget)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTarget);

        return controller.Json(new
        {
            success = true,
            refresh = refreshTarget
        });
    }

    public static PartialViewResult ModalForm(this Controller controller, string partialName, object? model = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partialName);

        return controller.PartialView(partialName, model);
    }
}
