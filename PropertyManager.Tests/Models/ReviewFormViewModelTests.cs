using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using PropertyManager.AppService.Reviews;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Models;

public class ReviewFormViewModelTests
{
    [Theory]
    [InlineData(ReviewOutcome.Return)]
    [InlineData(ReviewOutcome.Deny)]
    public void Validate_RequiresComment_ForReturnAndDeny(ReviewOutcome outcome)
    {
        var model = new ReviewFormViewModel
        {
            Outcome = outcome,
            Comment = "   "
        };

        var results = ValidateModel(model);

        results.Should().ContainSingle(r => r.MemberNames.Contains(nameof(ReviewFormViewModel.Comment)));
    }

    [Fact]
    public void Validate_AllowsEmptyComment_ForApprove()
    {
        var model = new ReviewFormViewModel
        {
            Outcome = ReviewOutcome.Approve,
            Comment = null
        };

        var results = ValidateModel(model);

        results.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateModel(ReviewFormViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
