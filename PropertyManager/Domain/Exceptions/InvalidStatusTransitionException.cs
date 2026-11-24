using System.Runtime.Serialization;

namespace PropertyManager.Domain.Exceptions;

public class InvalidStatusTransitionException(ApplicationStatus status, ApplicationAction action) : Exception($"Cannot apply action '{action}' to an application in status '{status}' status.")
{
}