using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Exceptions;
using PropertyManager.AppService.Reviews;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Controllers;

[Authorize(Roles = RoleNames.PropertyManager)]
public class ReviewsController(IReviewService reviewService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Review(Guid id, CancellationToken cancellationToken)
    {
        var review = await reviewService.GetReviewAsync(id, cancellationToken);
        if (review.Status != ApplicationStatus.Submitted)
        {
            throw new InvalidStatusTransitionException(review.Status, ApplicationAction.Approve);
        }

        return this.ModalForm("_ReviewForm", ReviewFormViewModel.FromDto(review));
    }

    [HttpPost]
    public async Task<IActionResult> Review(
        Guid id,
        ReviewFormViewModel model,
        CancellationToken cancellationToken)
    {
        var review = await reviewService.GetReviewAsync(id, cancellationToken);
        model.WithDetails(review);

        if (!ModelState.IsValid)
        {
            return this.ModalForm("_ReviewForm", model);
        }

        try
        {
            await reviewService.ReviewAsync(
                id,
                model.Outcome!.Value,
                model.Comment,
                cancellationToken);

            return RedirectToAction(
                nameof(ApplicationsController.Edit),
                "Applications",
                new { id, section = ApplicationSection.Summary });
        }
        catch (ReviewCommentRequiredException ex)
        {
            ModelState.AddModelError(nameof(ReviewFormViewModel.Comment), ex.Message);
        }
        catch (Exception ex) when (ex is DomainException or ConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return this.ModalForm("_ReviewForm", model);
    }
}
