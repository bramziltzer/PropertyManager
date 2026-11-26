using FluentAssertions;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Models;

public sealed class ResidenceFormViewModelTests
{
    [Fact]
    public void Validate_MoveOutBeforeMoveIn_ReturnsDateError()
    {
        var model = new ResidenceFormViewModel
        {
            MoveInDate = new DateOnly(2024, 6, 1),
            MoveOutDate = new DateOnly(2024, 5, 1)
        };

        var errors = model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model));

        errors.Should().ContainSingle(error =>
            error.MemberNames.Contains(nameof(ResidenceFormViewModel.MoveOutDate)) &&
            error.ErrorMessage!.Contains("before"));
    }
}
