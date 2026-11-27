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

                model.PopulatePageState(page, ApplicationSection.ApplicantInfo);
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
            await rentalApplicationService.SaveResidenceHistoryAsync(id, cancellationToken);
            return RedirectToAction(nameof(Edit), new { id, section = ApplicationSection.Summary });
        }

        if (currentSection == ApplicationSection.Summary &&
            string.Equals(command, "submit", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await rentalApplicationService.SubmitAsync(id, cancellationToken);
                return RedirectToAction(nameof(Edit), new { id, section = ApplicationSection.Summary });
            }
            catch (DomainException exception) when (exception is ApplicationIncompleteException or UnitNotAvailableException)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
                var page = await rentalApplicationService.GetApplicationPageAsync(
                    id,
                    ApplicationSection.Summary,
                    cancellationToken);
                model.PopulatePageState(page, ApplicationSection.Summary);
                return View(model);
            }
        }

        return RedirectToAction(nameof(Edit), new { id, section = currentSection });
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken cancellationToken)
    {
        var page = await rentalApplicationService.GetApplicationPageAsync(id, null, cancellationToken);
        if (!ApplicationStatusRules.OpenStatuses.Contains(page.Status))
        {
            throw new InvalidStatusTransitionException(page.Status, ApplicationAction.Withdraw);
        }

        return this.ModalForm("_WithdrawConfirm", new WithdrawConfirmViewModel
        {
            RentalApplicationId = page.RentalApplicationId,
            PropertyName = page.Unit.PropertyName,
            UnitNumber = page.Unit.UnitNumber,
            Status = page.Status
        });
    }

    [HttpPost]
    [ActionName("Withdraw")]
    [Authorize(Roles = RoleNames.Applicant)]
    public async Task<IActionResult> WithdrawConfirmed(Guid id, CancellationToken cancellationToken)
    {
        await rentalApplicationService.WithdrawAsync(id, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id, section = ApplicationSection.Summary });
    }
}
