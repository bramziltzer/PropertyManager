namespace PropertyManager.Domain.Rules;

public static class LeaseRules
{
    public static bool IsUnitAvailableForLease(RentalUnit unit, DateOnly currentDate) => unit.GetActiveLease(currentDate) == null;
}
