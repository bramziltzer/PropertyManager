using FluentAssertions;
using PropertyManager.Domain;
using PropertyManager.Domain.Rules;

namespace PropertyManager.Tests.Domain;

public sealed class BusinessRulesTests
{
    [Theory]
    [InlineData("+1 (555) 123-4567", true)]
    [InlineData("555.123.4567", true)]
    [InlineData("555 123 4567", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("555-123", false)]
    [InlineData("+1-555-CALL-NOW", false)]
    [InlineData("1234567890123456", false)]
    public void PhoneNumberRules_ValidateSupportedFormats(string? phoneNumber, bool isValid)
    {
        PhoneNumberRules.IsValid(phoneNumber).Should().Be(isValid);
    }

    [Theory]
    [InlineData(ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Denied, true)]
    [InlineData(ApplicationStatus.Draft, false)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void StatusRules_RequireCommentOnlyForReturnAndDeny(ApplicationStatus status, bool requiresComment)
    {
        ApplicationStatusRules.RequiresComment(status).Should().Be(requiresComment);
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, true)]
    [InlineData(ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Denied, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void StatusRules_EditabilityMatchesDomainStatuses(ApplicationStatus status, bool isEditable)
    {
        ApplicationStatusRules.IsEditable(status).Should().Be(isEditable);
    }

    [Theory]
    [InlineData(2026, 6, 1, true)]
    [InlineData(2027, 5, 31, true)]
    [InlineData(2027, 6, 1, false)]
    [InlineData(2026, 5, 31, false)]
    public void LeaseRules_IsActiveOnIncludesBothTermEndpoints(int year, int month, int day, bool expected)
    {
        var lease = TestData.Submitted().Approve(
            TestData.ManagerId,
            TestData.Now,
            TestData.Today);

        LeaseRules.IsActiveOn(lease, new DateOnly(year, month, day)).Should().Be(expected);
    }

    [Theory]
    [InlineData(2026, 6, 1, 2027, 5, 31, true)]
    [InlineData(2027, 5, 31, 2027, 7, 31, true)]
    [InlineData(2027, 6, 1, 2027, 7, 31, false)]
    [InlineData(2025, 6, 1, 2026, 5, 31, false)]
    public void LeaseRules_OverlapsIncludesAdjacentBoundaries(
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay,
        bool expected)
    {
        var lease = TestData.Submitted().Approve(
            TestData.ManagerId,
            TestData.Now,
            TestData.Today);

        LeaseRules.Overlaps(
                lease,
                new DateOnly(startYear, startMonth, startDay),
                new DateOnly(endYear, endMonth, endDay))
            .Should().Be(expected);
    }
}
