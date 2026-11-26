using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertyManager.AppService.Applications;
using PropertyManager.AppService.Exceptions;
using PropertyManager.AppService.Reviews;
using PropertyManager.Controllers;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Controllers;

public class ReviewsControllerTests
{
    [Fact]
    public async Task ReviewGet_ReturnsModalForm_WhenSubmitted()
    {
        var reviewService = new Mock<IReviewService>();
        var applicationId = Guid.NewGuid();
        reviewService
            .Setup(s => s.GetReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDetailsDto(
                applicationId,
                ApplicationStatus.Submitted,
                "Oakridge",
                "101",
                "Ada Lovelace"));

        var controller = new ReviewsController(reviewService.Object);

        var result = await controller.Review(applicationId, CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_ReviewForm");
        var model = partial.Model.Should().BeOfType<ReviewFormViewModel>().Subject;
        model.RentalApplicationId.Should().Be(applicationId);
        model.PropertyName.Should().Be("Oakridge");
        model.UnitNumber.Should().Be("101");
        model.ApplicantDisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task ReviewGet_ThrowsInvalidStatusTransitionException_WhenNotSubmitted()
    {
        var reviewService = new Mock<IReviewService>();
        var applicationId = Guid.NewGuid();
        reviewService
            .Setup(s => s.GetReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDetailsDto(
                applicationId,
                ApplicationStatus.Draft,
                "Oakridge",
                "101",
                "Ada Lovelace"));

        var controller = new ReviewsController(reviewService.Object);

        await controller.Invoking(c => c.Review(applicationId, CancellationToken.None))
            .Should().ThrowAsync<InvalidStatusTransitionException>();
    }

    [Fact]
    public async Task ReviewPost_InvalidModelState_ReturnsModalForm()
    {
        var reviewService = new Mock<IReviewService>();
        var applicationId = Guid.NewGuid();
        reviewService
            .Setup(s => s.GetReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDetailsDto(
                applicationId,
                ApplicationStatus.Submitted,
                "Oakridge",
                "101",
                "Ada Lovelace"));

        var controller = new ReviewsController(reviewService.Object);
        controller.ModelState.AddModelError(nameof(ReviewFormViewModel.Outcome), "Select a review outcome.");

        var result = await controller.Review(
            applicationId,
            new ReviewFormViewModel(),
            CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_ReviewForm");
        reviewService.Verify(
            s => s.ReviewAsync(It.IsAny<Guid>(), It.IsAny<ReviewOutcome>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReviewPost_ReviewCommentRequiredException_AddsCommentErrorAndReturnsModalForm()
    {
        var reviewService = new Mock<IReviewService>();
        var applicationId = Guid.NewGuid();
        reviewService
            .Setup(s => s.GetReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDetailsDto(
                applicationId,
                ApplicationStatus.Submitted,
                "Oakridge",
                "101",
                "Ada Lovelace"));
        reviewService
            .Setup(s => s.ReviewAsync(applicationId, ReviewOutcome.Return, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ReviewCommentRequiredException(ApplicationStatus.Returned));

        var controller = new ReviewsController(reviewService.Object);

        var result = await controller.Review(
            applicationId,
            new ReviewFormViewModel { Outcome = ReviewOutcome.Return },
            CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_ReviewForm");
        controller.ModelState[nameof(ReviewFormViewModel.Comment)]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task ReviewPost_ConcurrencyException_AddsModelErrorAndReturnsModalForm()
    {
        var reviewService = new Mock<IReviewService>();
        var applicationId = Guid.NewGuid();
        reviewService
            .Setup(s => s.GetReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDetailsDto(
                applicationId,
                ApplicationStatus.Submitted,
                "Oakridge",
                "101",
                "Ada Lovelace"));
        reviewService
            .Setup(s => s.ReviewAsync(applicationId, ReviewOutcome.Approve, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyException());

        var controller = new ReviewsController(reviewService.Object);

        var result = await controller.Review(
            applicationId,
            new ReviewFormViewModel { Outcome = ReviewOutcome.Approve },
            CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_ReviewForm");
        controller.ModelState[string.Empty]!.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task ReviewPost_Valid_ReviewsAndRedirectsToSummary()
    {
        var reviewService = new Mock<IReviewService>();
        var applicationId = Guid.NewGuid();
        reviewService
            .Setup(s => s.GetReviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDetailsDto(
                applicationId,
                ApplicationStatus.Submitted,
                "Oakridge",
                "101",
                "Ada Lovelace"));

        var controller = new ReviewsController(reviewService.Object);

        var result = await controller.Review(
            applicationId,
            new ReviewFormViewModel
            {
                Outcome = ReviewOutcome.Approve,
                Comment = "Approved"
            },
            CancellationToken.None);

        reviewService.Verify(
            s => s.ReviewAsync(applicationId, ReviewOutcome.Approve, "Approved", It.IsAny<CancellationToken>()),
            Times.Once);
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(ApplicationsController.Edit));
        redirect.ControllerName.Should().Be("Applications");
        redirect.RouteValues!["id"].Should().Be(applicationId);
        redirect.RouteValues["section"].Should().Be(ApplicationSection.Summary);
    }
}
