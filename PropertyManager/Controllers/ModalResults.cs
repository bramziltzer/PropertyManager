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

    /// <summary>
    /// Returns a partial view result for a modal form.
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="partialName"></param>
    /// <param name="model"></param>
    /// <returns></returns>
    public static PartialViewResult ModalForm(this Controller controller, string partialName, object? model = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partialName);

        return controller.PartialView(partialName, model);
    }
}
