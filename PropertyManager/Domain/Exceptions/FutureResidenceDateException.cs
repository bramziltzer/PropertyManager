namespace PropertyManager.Domain.Exceptions;

public class FutureResidenceDateException()
    : DomainException("Residence dates cannot be in the future.");
