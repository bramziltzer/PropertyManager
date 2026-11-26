using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Applications;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Controllers;

[Authorize(Roles = RoleNames.Applicant)]
public class ResidencesController(IRentalApplicationService rentalApplicationService) : Controller
{
    private static string ResidencesRefreshRegion(Guid applicationId) =>
        $"application-residences-{applicationId}";

    [HttpGet]
    public async Task<IActionResult> List(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await rentalApplicationService.GetApplicationResidencesAsync(applicationId, cancellationToken);
        return ViewComponent(
            "ResidenceHistorySection",
            new
            {
                applicationId,
                residences = result.Residences,
                isEditable = result.IsEditable
            });
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid applicationId, CancellationToken cancellationToken)
    {
        await EnsureApplicationEditableAsync(applicationId, cancellationToken);
        return this.ModalForm("_ResidenceForm", new ResidenceFormViewModel
        {
            RentalApplicationId = applicationId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid applicationId,
        ResidenceFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.RentalApplicationId = applicationId;
        await EnsureApplicationEditableAsync(applicationId, cancellationToken);

        if (!ModelState.IsValid)
        {
            return this.ModalForm("_ResidenceForm", model);
        }

        await rentalApplicationService.AddResidenceAsync(applicationId, model.ToDto(), cancellationToken);
        return this.ModalSuccess(ResidencesRefreshRegion(applicationId));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        Guid applicationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await EnsureApplicationEditableAsync(applicationId, cancellationToken);
        var residence = await rentalApplicationService.GetResidenceAsync(
            applicationId,
            id,
            cancellationToken);

        return this.ModalForm("_ResidenceForm", ResidenceFormViewModel.FromDto(applicationId, residence));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        Guid applicationId,
        Guid id,
        ResidenceFormViewModel model,
        CancellationToken cancellationToken)
    {
        model.RentalApplicationId = applicationId;
        model.ResidenceHistoryId = id;
        await EnsureApplicationEditableAsync(applicationId, cancellationToken);

        if (!ModelState.IsValid)
        {
            return this.ModalForm("_ResidenceForm", model);
        }

        await rentalApplicationService.UpdateResidenceAsync(
            applicationId,
            id,
            model.ToDto(),
            cancellationToken);
        return this.ModalSuccess(ResidencesRefreshRegion(applicationId));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(
        Guid applicationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await EnsureApplicationEditableAsync(applicationId, cancellationToken);
        var residence = await rentalApplicationService.GetResidenceAsync(
            applicationId,
            id,
            cancellationToken);

        return this.ModalForm("Modal/_ConfirmDelete", new ConfirmDeleteViewModel
        {
            ActionUrl = Url.Action(nameof(Delete), new { applicationId, id })!,
            EntityName = "Residence",
            Detail = $"{AddressFormatter.Format(residence.Address)}, {residence.MoveInDate:d} - {(residence.MoveOutDate?.ToString("d") ?? "Present")}"
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(
        Guid applicationId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await EnsureApplicationEditableAsync(applicationId, cancellationToken);
        await rentalApplicationService.RemoveResidenceAsync(applicationId, id, cancellationToken);
        return this.ModalSuccess(ResidencesRefreshRegion(applicationId));
    }

    private async Task EnsureApplicationEditableAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var residences = await rentalApplicationService.GetApplicationResidencesAsync(
            applicationId,
            cancellationToken);

        if (!residences.IsEditable)
        {
            throw new ApplicationNotEditableException(residences.Status);
        }
    }
}
