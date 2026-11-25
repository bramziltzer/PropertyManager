using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Tests.Domain;

public sealed class RentalApplicationTests
{
    [Fact]
    public void StatusChanges_PreserveCommentsOnEveryChange()
    {
        var application = TestData.Submitted();

        application.Return(TestData.ManagerId, TestData.Now.AddDays(1), "Please provide a reference.");
        application.Submit(TestData.ApplicantId, TestData.Now.AddDays(2), TestData.Today);
        application.Deny(TestData.ManagerId, TestData.Now.AddDays(3), "Application denied.");

        application.History.Select(change => change.Comment)
            .Should()
            .Equal(null, "Please provide a reference.", null, "Application denied.");
        application.CurrentComment.Should().Be("Application denied.");
    }

    [Fact]
    public void CurrentComment_IsNullWhenLatestStatusChangeHasNoComment()
    {
        var application = TestData.Submitted();

        application.Return(TestData.ManagerId, TestData.Now.AddDays(1), "Please provide a reference.");
        application.Submit(TestData.ApplicantId, TestData.Now.AddDays(2), TestData.Today);

        application.CurrentComment.Should().BeNull();
    }

    [Fact]
    public void Model_BuildsAndDoesNotMapDerivedProperties()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        using var db = new ApplicationDbContext(options);

        var entity = db.Model.FindEntityType(typeof(RentalApplication))!;

        entity.FindProperty(nameof(RentalApplication.CurrentComment)).Should().BeNull();
        entity.FindProperty(nameof(RentalApplication.IsEditable)).Should().BeNull();
        entity.FindNavigation(nameof(RentalApplication.ResidenceHistories))!.FieldInfo.Should().NotBeNull();
    }

    [Fact]
    public void StartDraft_RecordsOwnerAndStartsInDraft()
    {
        var application = TestData.Draft();

        application.Status.Should().Be(ApplicationStatus.Draft);
        application.IsOwnedBy(TestData.ApplicantId).Should().BeTrue();
        application.IsOwnedBy(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void StartDraft_RejectsUnitWithActiveLease()
    {
        var unit = TestData.Unit();
        TestData.Submitted(unit).Approve(TestData.ManagerId, TestData.Now, TestData.Today);

        var act = () => TestData.Draft(unit);

        act.Should().Throw<UnitNotAvailableException>();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft, true)]
    [InlineData(ApplicationStatus.Returned, true)]
    [InlineData(ApplicationStatus.Submitted, false)]
    [InlineData(ApplicationStatus.Approved, false)]
    [InlineData(ApplicationStatus.Denied, false)]
    [InlineData(ApplicationStatus.Withdrawn, false)]
    public void Sections_AreEditableOnlyInDraftOrReturned(ApplicationStatus status, bool editable)
    {
        var application = TestData.InStatus(status);

        application.IsEditable.Should().Be(editable);
        var act = () => application.SaveApplicantInfo(TestData.Info());
        if (editable)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<ApplicationNotEditableException>();
        }
    }

    [Fact]
    public void ResidenceChanges_AreRejectedWhenNotEditable()
    {
        var application = TestData.Submitted();
        var residence = application.ResidenceHistories.Single();

        application.Invoking(a => a.AddResidence(TestData.Address(), "L", "555", new DateOnly(2020, 1, 1), null))
            .Should().Throw<ApplicationNotEditableException>();
        application.Invoking(a => a.        RemoveResidence(residence.ResidenceHistoryId))
            .Should().Throw<ApplicationNotEditableException>();
    }

    [Fact]
    public void Submit_RequiresBothSectionsToBeSaved()
    {
        var application = TestData.Draft();

        var act = () => application.Submit(TestData.ApplicantId, TestData.Now, TestData.Today);

        act.Should().Throw<ApplicationIncompleteException>()
            .Which.IncompleteSections.Should().Equal("Applicant Information", "Residence History");
    }

    [Fact]
    public void ResidenceHistory_IsSavedWhileItHasAtLeastOneResidence()
    {
        var application = TestData.Draft();
        application.IsResidenceHistorySaved.Should().BeFalse();

        var residence = application.AddResidence(TestData.Address(), "Landlord", "555-0102", new DateOnly(2015, 1, 1), null);
        application.IsResidenceHistorySaved.Should().BeTrue();

        application.RemoveResidence(residence.ResidenceHistoryId);
        application.IsResidenceHistorySaved.Should().BeFalse();
    }

    [Fact]
    public void UpdateAndRemoveResidence_ChangeTheList()
    {
        var application = TestData.Completed();
        var residence = application.ResidenceHistories.Single();

        application.UpdateResidence(residence.ResidenceHistoryId, TestData.Address(), " New Landlord ", "555-0103", new DateOnly(2019, 1, 1), new DateOnly(2020, 1, 1));
        residence.LandlordName.Should().Be("New Landlord");
        application.IsReadyToSubmit.Should().BeTrue();

        application.RemoveResidence(residence.ResidenceHistoryId);
        application.ResidenceHistories.Should().BeEmpty();
    }

    [Fact]
    public void UpdateAndRemoveResidence_RejectUnknownResidence()
    {
        var application = TestData.Completed();
        var unknownId = Guid.NewGuid();

        application.Invoking(a => a.UpdateResidence(unknownId, TestData.Address(), "L", "555", new DateOnly(2019, 1, 1), null))
            .Should().Throw<ResidenceNotFoundException>();
        application.Invoking(a => a.RemoveResidence(unknownId))
            .Should().Throw<ResidenceNotFoundException>();
    }

    [Fact]
    public void AddResidence_RejectsMoveOutBeforeMoveIn()
    {
        var application = TestData.Draft();

        application.Invoking(a => a.AddResidence(TestData.Address(), "L", "555", new DateOnly(2020, 1, 1), new DateOnly(2019, 1, 1)))
            .Should().Throw<InvalidResidenceDatesException>();
    }

    [Fact]
    public void Submit_RejectedWhenUnitHasActiveLease()
    {
        var unit = TestData.Unit();
        var first = TestData.Submitted(unit);
        var second = TestData.Completed(unit, Guid.NewGuid());
        first.Approve(TestData.ManagerId, TestData.Now, TestData.Today);

        var act = () => second.Submit(Guid.NewGuid(), TestData.Now, TestData.Today);

        act.Should().Throw<UnitNotAvailableException>();
        second.Status.Should().Be(ApplicationStatus.Draft);
    }

    [Fact]
    public void Approve_IssuesTwelveMonthLeaseForTheApplicant()
    {
        var application = TestData.Submitted();

        var lease = application.Approve(TestData.ManagerId, TestData.Now, TestData.Today);

        application.Status.Should().Be(ApplicationStatus.Approved);
        application.Lease.Should().BeSameAs(lease);
        lease.LesseeId.Should().Be(TestData.ApplicantId);
        lease.BeginDate.Should().Be(new DateOnly(2026, 6, 1));
        lease.EndDate.Should().Be(new DateOnly(2027, 5, 31));
        application.Unit.Leases.Should().ContainSingle();
    }

    [Fact]
    public void Approve_RejectsSecondLeaseAndLeavesOtherApplicationsAlone()
    {
        var unit = TestData.Unit();
        var first = TestData.Submitted(unit, Guid.NewGuid());
        var second = TestData.Submitted(unit, Guid.NewGuid());
        first.Approve(TestData.ManagerId, TestData.Now, TestData.Today);

        var act = () => second.Approve(TestData.ManagerId, TestData.Now, TestData.Today);

        act.Should().Throw<UnitNotAvailableException>();
        second.Status.Should().Be(ApplicationStatus.Submitted);
        second.Lease.Should().BeNull();
        unit.Leases.Should().ContainSingle();
    }

    [Theory]
    [InlineData(ApplicationAction.Return)]
    [InlineData(ApplicationAction.Deny)]
    public void ReturnAndDeny_RequireComment(ApplicationAction action)
    {
        var application = TestData.Submitted();

        Action act = action == ApplicationAction.Return
            ? () => application.Return(TestData.ManagerId, TestData.Now, "  ")
            : () => application.Deny(TestData.ManagerId, TestData.Now, null);

        act.Should().Throw<ReviewCommentRequiredException>();
        application.Status.Should().Be(ApplicationStatus.Submitted);
    }

    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Denied)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public void TerminalStatuses_RejectFurtherActions(ApplicationStatus status)
    {
        var application = TestData.InStatus(status);

        application.Invoking(a => a.Withdraw(TestData.ApplicantId, TestData.Now))
            .Should().Throw<InvalidStatusTransitionException>();
        application.Invoking(a => a.Approve(TestData.ManagerId, TestData.Now, TestData.Today))
            .Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void ReturnedApplication_CanBeCorrectedAndResubmitted()
    {
        var application = TestData.InStatus(ApplicationStatus.Returned);

        application.SaveApplicantInfo(TestData.Info());
        application.Submit(TestData.ApplicantId, TestData.Now.AddDays(1), TestData.Today);

        application.Status.Should().Be(ApplicationStatus.Submitted);
    }
}
