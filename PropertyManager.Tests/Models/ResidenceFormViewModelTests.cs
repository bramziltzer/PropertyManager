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

    [Fact]
    public void Validate_FutureDates_ReturnsDateErrors()
    {
        var model = new ResidenceFormViewModel
        {
            LandlordPhone = "555-0100",
            MoveInDate = new DateOnly(9999, 1, 1),
            MoveOutDate = new DateOnly(9999, 1, 2)
        };

        var errors = model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model));

        errors.Should().Contain(error =>
            error.MemberNames.Contains(nameof(ResidenceFormViewModel.MoveInDate)));
        errors.Should().Contain(error =>
            error.MemberNames.Contains(nameof(ResidenceFormViewModel.MoveOutDate)));
    }

    [Theory]
    [InlineData("555-0100")]
    [InlineData("(555) 555-0100")]
    [InlineData("+1 555 555 0100")]
    public void Validate_ValidPhoneNumber_ReturnsNoPhoneError(string phoneNumber)
    {
        var model = new ResidenceFormViewModel
        {
            LandlordPhone = phoneNumber
        };

        var errors = model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model));

        errors.Should().NotContain(error =>
            error.MemberNames.Contains(nameof(ResidenceFormViewModel.LandlordPhone)));
    }

    [Theory]
    [InlineData("call-me-now")]
    [InlineData("123")]
    [InlineData("555-ABC-0100")]
    public void Validate_InvalidPhoneNumber_ReturnsPhoneError(string phoneNumber)
    {
        var model = new ResidenceFormViewModel
        {
            LandlordPhone = phoneNumber
        };

        var errors = model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model));

        errors.Should().Contain(error =>
            error.MemberNames.Contains(nameof(ResidenceFormViewModel.LandlordPhone)));
    }

    [Fact]
    public void ApplicantInfoValidate_InvalidPhoneNumber_ReturnsPhoneError()
    {
        var model = new ApplicantInfoSectionViewModel
        {
            PhoneNumber = "call-me-now"
        };

        var errors = model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model));

        errors.Should().Contain(error =>
            error.MemberNames.Contains(nameof(ApplicantInfoSectionViewModel.PhoneNumber)));
    }
}
