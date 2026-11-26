namespace PropertyManager.Domain.Exceptions;

public class DuplicateOpenApplicationException()
    : DomainException("You already have an open application for this unit.");
