namespace PropertyManager.Domain.Rules;

public static class LeaseRules
{
    public static bool IsUnitAvailableForLease(RentalUnit unit, DateTime currentDate) => unit.GetActiveLease(currentDate) == null;
}
