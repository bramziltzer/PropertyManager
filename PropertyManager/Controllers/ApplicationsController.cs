using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Applications;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
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

    [HttpGet]
    public async Task<IActionResult> Edit(
        Guid id,
        ApplicationSection? section,
        CancellationToken cancellationToken)
    {
        var page = await rentalApplicationService.GetApplicationPageAsync(id, section, cancellationToken);
        return View(ApplicationPageViewModel.FromDto(page));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        Guid id,
        ApplicationSection currentSection,
        string command,
        ApplicationPageViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.Equals(command, "back", StringComparison.OrdinalIgnoreCase))
        {
            var previousSection = currentSection switch
            {
                ApplicationSection.ResidenceHistory => ApplicationSection.ApplicantInfo,
                ApplicationSection.Summary => ApplicationSection.ResidenceHistory,
                _ => ApplicationSection.ApplicantInfo
            };

            return RedirectToAction(nameof(Edit), new { id, section = previousSection });
        }

        if (currentSection == ApplicationSection.ApplicantInfo &&
            string.Equals(command, "continue", StringComparison.OrdinalIgnoreCase))
        {
            if (!ModelState.IsValid)
            {
                var page = await rentalApplicationService.GetApplicationPageAsync(id, currentSection, cancellationToken);
                if (!page.IsEditable)
                {
                    throw new ApplicationNotEditableException(page.Status);
                }

                model.ApplyPageContext(page, ApplicationSection.ApplicantInfo);
                return View(model);
            }

            await rentalApplicationService.SaveApplicantInfoAsync(
                id,
                model.ApplicantInfo.ToDto(),
                cancellationToken);

            return RedirectToAction(nameof(Edit), new { id, section = ApplicationSection.ResidenceHistory });
        }

        if (currentSection == ApplicationSection.ResidenceHistory &&
            string.Equals(command, "continue", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await rentalApplicationService.EnsureResidenceHistoryCompleteAsync(id, cancellationToken);
            }
            catch (ApplicationIncompleteException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                var page = await rentalApplicationService.GetApplicationPageAsync(
                    id,
                    ApplicationSection.ResidenceHistory,
                    cancellationToken);
                model.ApplyPageContext(page, ApplicationSection.ResidenceHistory);
                return View(model);
            }

            return RedirectToAction(nameof(Edit), new { id, section = ApplicationSection.Summary });
        }

        return RedirectToAction(nameof(Edit), new { id, section = currentSection });
    }
}
