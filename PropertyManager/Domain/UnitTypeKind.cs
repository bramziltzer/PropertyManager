namespace PropertyManager.Domain;

// Values are persisted as the lookup table's primary key. Never renumber or reuse a value.
public enum UnitTypeKind
{
    Apartment = 1,
    House = 2,
    Condo = 3,
    Townhouse = 4
}

public static class UnitTypeKindUtils
{
    public static List<UnitTypeKind> GetAllKinds() => Enum.GetValues<UnitTypeKind>().ToList();
}
