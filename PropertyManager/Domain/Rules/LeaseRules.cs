using System.Linq.Expressions;

namespace PropertyManager.Domain.Rules;

public static class LeaseRules
{
    // TODO consider making this configurable
    public const int TermMonths = 12;

    /// <summary>
    /// The last day covered by a lease that begins on <paramref name="startDate"/>.
    /// </summary>
    public static DateOnly TermEnd(DateOnly startDate) => startDate.AddMonths(TermMonths).AddDays(-1);

    public static bool IsActiveOn(RentalLease lease, DateOnly date) => lease.BeginDate <= date && lease.EndDate >= date;

    public static bool Overlaps(RentalLease lease, DateOnly start, DateOnly end) => lease.BeginDate <= end && lease.EndDate >= start;

    public static bool IsUnitAvailableForLease(RentalUnit unit, DateOnly currentDate) => unit.GetActiveLease(currentDate) == null;

    /// <summary>
    /// Database-translatable form of <see cref="IsUnitAvailableForLease"/>; keep the two in sync.
    /// </summary>
    public static Expression<Func<RentalUnit, bool>> IsAvailableOn(DateOnly date) =>
        unit => !unit.Leases.Any(lease => lease.BeginDate <= date && lease.EndDate >= date);
}
