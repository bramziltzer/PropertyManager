using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertyManager.AppService;
using PropertyManager.AppService.Applications;
using PropertyManager.Controllers;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Controllers;

public sealed class ResidencesControllerTests
{
    [Fact]
    public async Task CreatePost_Valid_ReturnsRefreshForResidenceRegion()
    {
        var applicationId = Guid.NewGuid();
        var residenceId = Guid.NewGuid();
        var service = new Mock<IRentalApplicationService>();
        service.Setup(item => item.GetResidencesAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidenceListDto(applicationId, [], ApplicationStatus.Draft, true));
        service.Setup(item => item.AddResidenceAsync(
                applicationId,
                It.Is<SaveResidenceDto>(dto => dto.LandlordName == "Landlord"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(residenceId);
        var controller = new ResidencesController(service.Object);

        var result = await controller.Create(
            applicationId,
            CreateModel(applicationId),
            CancellationToken.None);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        JsonSerializer.Serialize(json.Value).Should().Be(
            $$"""{"success":true,"refresh":"application-residences-{{applicationId}}"}""");
    }

    [Fact]
    public async Task CreatePost_InvalidDates_RendersFormWithoutPersisting()
    {
        var applicationId = Guid.NewGuid();
        var service = new Mock<IRentalApplicationService>();
        service.Setup(item => item.GetResidencesAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidenceListDto(applicationId, [], ApplicationStatus.Draft, true));
        var controller = new ResidencesController(service.Object);
        controller.ModelState.AddModelError(
            nameof(ResidenceFormViewModel.MoveOutDate),
            "Move-out date cannot be before the move-in date.");

        var result = await controller.Create(
            applicationId,
            CreateModel(applicationId, moveOut: new DateOnly(2019, 1, 1)),
            CancellationToken.None);

        var partial = result.Should().BeOfType<PartialViewResult>().Subject;
        partial.ViewName.Should().Be("_ResidenceForm");
        controller.ModelState.Should().ContainKey(nameof(ResidenceFormViewModel.MoveOutDate));
        service.Verify(item => item.AddResidenceAsync(
            It.IsAny<Guid>(),
            It.IsAny<SaveResidenceDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EditPost_NonEditableApplication_IsRejectedBeforePersistence()
    {
        var applicationId = Guid.NewGuid();
        var service = new Mock<IRentalApplicationService>();
        service.Setup(item => item.GetResidencesAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidenceListDto(
                applicationId,
                [],
                ApplicationStatus.Submitted,
                false));
        var controller = new ResidencesController(service.Object);

        await controller.Invoking(item => item.Edit(
                applicationId,
                Guid.NewGuid(),
                CreateModel(applicationId),
                CancellationToken.None))
            .Should().ThrowAsync<ApplicationNotEditableException>();

        service.Verify(item => item.UpdateResidenceAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<SaveResidenceDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task List_ReturnsResidenceHistoryComponent()
    {
        var applicationId = Guid.NewGuid();
        var residences = new List<ResidenceDto>
        {
            new(
                Guid.NewGuid(),
                new AddressDto("10 Main St", null, "Boston", "MA", "02101"),
                "Landlord",
                "555-0100",
                new DateOnly(2020, 1, 1),
                null)
        };
        var service = new Mock<IRentalApplicationService>();
        service.Setup(item => item.GetResidencesAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResidenceListDto(applicationId, residences, ApplicationStatus.Draft, true));
        var controller = new ResidencesController(service.Object);

        var result = await controller.List(applicationId, CancellationToken.None);

        result.Should().BeOfType<ViewComponentResult>()
            .Which.ViewComponentName.Should().Be("ResidenceHistorySection");
    }

    private static ResidenceFormViewModel CreateModel(
        Guid applicationId,
        DateOnly? moveOut = null) => new()
    {
        RentalApplicationId = applicationId,
        Address = new AddressViewModel
        {
            Line1 = "10 Main St",
            City = "Boston",
            State = "MA",
            ZipCode = "02101"
        },
        LandlordName = "Landlord",
        LandlordPhone = "555-0100",
        MoveInDate = new DateOnly(2020, 1, 1),
        MoveOutDate = moveOut
    };
}
