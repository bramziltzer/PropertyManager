namespace PropertyManager.Domain.Exceptions;

public class DuplicateUnitNumberException(string unitNumber)
    : DomainException($"Unit number '{unitNumber}' already exists on this property.");
