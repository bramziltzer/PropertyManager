using FluentAssertions;
using PropertyManager.Domain;
using PropertyManager.Models.ApplicationModels;

namespace PropertyManager.Tests.Models;

public sealed class ApplicationStatusChangeViewModelTests
{
    [Theory]
    [InlineData(ApplicationStatus.Submitted, "Submit")]
    [InlineData(ApplicationStatus.Returned, "Return")]
    [InlineData(ApplicationStatus.Approved, "Approve")]
    [InlineData(ApplicationStatus.Denied, "Deny")]
    [InlineData(ApplicationStatus.Withdrawn, "Withdraw")]
    [InlineData(ApplicationStatus.Draft, "Draft")]
    public void Outcome_FormatsStatusForDisplay(ApplicationStatus status, string expected)
    {
        var model = new ApplicationStatusChangeViewModel(
            Guid.NewGuid(),
            ApplicationStatus.Draft,
            status,
            null,
            "manager@example.com",
            DateTimeOffset.UtcNow);

        model.Outcome.Should().Be(expected);
    }
}
