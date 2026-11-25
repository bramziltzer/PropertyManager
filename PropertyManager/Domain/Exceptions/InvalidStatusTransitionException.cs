namespace PropertyManager.Domain.Exceptions;

public class InvalidStatusTransitionException(ApplicationStatus status, ApplicationAction action)
    : DomainException($"Cannot apply action '{action}' to an application in status '{status}'.");