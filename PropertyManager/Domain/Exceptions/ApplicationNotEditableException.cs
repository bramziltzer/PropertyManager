namespace PropertyManager.Domain.Exceptions;

public class ApplicationNotEditableException(ApplicationStatus status)
    : DomainException($"An application in status '{status}' cannot be edited.");
