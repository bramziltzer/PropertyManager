namespace PropertyManager.Domain.Exceptions;

public class InvalidResidenceDatesException()
    : DomainException("The move-out date cannot be before the move-in date.");
