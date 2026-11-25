using FluentAssertions;
using PropertyManager.Domain;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Domain.Rules;

namespace PropertyManager.Tests.Domain;

public sealed class PropertyAndUnitTests
{
    [Fact]
    public void NewUnit_CannotUseInactiveType()
    {
        var act = () => new RentalUnit("101", 1, 1000m, TestData.InactiveType());

        act.Should().Throw<InactiveUnitTypeException>();
    }

    [Fact]
    public void UpdateUnit_MayKeepAnInactiveTypeItAlreadyUses()
    {
        var inactive = TestData.InactiveType();
        var (property, unit) = PropertyWithUnit(inactive, activeAtCreation: true);

        property.UpdateUnit(unit, "101", 3, 2000m, inactive);

        unit.Bedrooms.Should().Be(3);
        unit.Kind.Should().Be(inactive.Kind);
    }

    [Fact]
    public void UpdateUnit_CannotSwitchToAnInactiveType()
    {
        var (property, unit) = PropertyWithUnit(TestData.ActiveType());

        var act = () => property.UpdateUnit(unit, "101", 2, 1500m, TestData.InactiveType());

        act.Should().Throw<InactiveUnitTypeException>();
        unit.Kind.Should().Be(UnitTypeKind.Apartment);
    }

    [Fact]
    public void UpdateUnit_CanSwitchFromAnInactiveTypeToAnActiveOne()
    {
        var inactive = TestData.InactiveType();
        var (property, unit) = PropertyWithUnit(inactive, activeAtCreation: true);

        property.UpdateUnit(unit, "101", 2, 1500m, TestData.ActiveType(UnitTypeKind.House));

        unit.Kind.Should().Be(UnitTypeKind.House);
    }

    [Fact]
    public void Units_MustHaveUniqueNumbersWithinAProperty()
    {
        var (property, unit) = PropertyWithUnit(TestData.ActiveType());
        var other = new RentalUnit("102", 1, 900m, TestData.ActiveType());
        property.AddUnit(other);

        property.Invoking(p => p.AddUnit(new RentalUnit(" 101 ", 1, 900m, TestData.ActiveType())))
            .Should().Throw<DuplicateUnitNumberException>();
        property.Invoking(p => p.UpdateUnit(other, "101", 1, 900m, TestData.ActiveType()))
            .Should().Throw<DuplicateUnitNumberException>();
        property.Invoking(p => p.UpdateUnit(unit, "101", 5, 900m, TestData.ActiveType()))
            .Should().NotThrow();
    }

    [Fact]
    public void RemoveUnit_IsBlockedWhenTheUnitHasALease()
    {
        var (property, unit) = PropertyWithUnit(TestData.ActiveType());
        TestData.Submitted(unit).Approve(TestData.ManagerId, TestData.Now, TestData.Today);

        property.Invoking(p => p.RemoveUnit(unit)).Should().Throw<RemovalBlockedException>();
        property.Units.Should().Contain(unit);
    }

    [Fact]
    public void RemoveUnit_DetachesAnUnusedUnit()
    {
        var (property, unit) = PropertyWithUnit(TestData.ActiveType());

        property.RemoveUnit(unit);

        property.Units.Should().BeEmpty();
    }

    [Fact]
    public void Property_CannotBeRemovedWhileItHasUnits()
    {
        var (property, unit) = PropertyWithUnit(TestData.ActiveType());

        property.Invoking(p => p.EnsureRemovable()).Should().Throw<RemovalBlockedException>();

        property.RemoveUnit(unit);
        property.Invoking(p => p.EnsureRemovable()).Should().NotThrow();
    }

    [Theory]
    [InlineData(2026, 6, 1, 2027, 5, 31)]
    [InlineData(2026, 1, 31, 2027, 1, 30)]
    [InlineData(2024, 2, 29, 2025, 2, 27)]
    public void LeaseTerm_IsTwelveMonthsInclusive(int y, int m, int d, int endY, int endM, int endD)
    {
        LeaseRules.TermEnd(new DateOnly(y, m, d)).Should().Be(new DateOnly(endY, endM, endD));
    }

    [Fact]
    public void Availability_PredicateAndExpressionAgree()
    {
        var unit = TestData.Unit();
        var application = TestData.Submitted(unit);
        application.Approve(TestData.ManagerId, TestData.Now, TestData.Today);
        var isAvailable = LeaseRules.IsAvailableOn(TestData.Today).Compile();

        foreach (var date in new[] { TestData.Today.AddDays(-1), TestData.Today, new DateOnly(2027, 5, 31), new DateOnly(2027, 6, 1) })
        {
            LeaseRules.IsUnitAvailableForLease(unit, date).Should().Be(unit.GetActiveLease(date) is null);
        }

        isAvailable(unit).Should().BeFalse();
        LeaseRules.IsAvailableOn(new DateOnly(2027, 6, 1)).Compile()(unit).Should().BeTrue();
        unit.IsAvailable(new DateOnly(2027, 5, 31)).Should().BeFalse();
        unit.IsAvailable(new DateOnly(2027, 6, 1)).Should().BeTrue();
    }

    private static (Property Property, RentalUnit Unit) PropertyWithUnit(UnitType type, bool activeAtCreation = false)
    {
        var property = new Property("Maple Court", TestData.Address());
        var creationType = activeAtCreation ? new UnitType { Kind = type.Kind, Name = type.Name, IsActive = true } : type;
        var unit = new RentalUnit("101", 2, 1500m, creationType);
        property.AddUnit(unit);
        return (property, unit);
    }
}
