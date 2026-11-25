namespace PropertyManager.Domain.Exceptions;

public class InactiveUnitTypeException(string unitTypeName)
    : DomainException($"Unit type '{unitTypeName}' is inactive and cannot be selected.");
