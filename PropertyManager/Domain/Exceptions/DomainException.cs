namespace PropertyManager.Domain.Exceptions;

/// <summary>
/// Base type for business-rule violations that can be reported to the user.
/// </summary>
public abstract class DomainException(string message) : Exception(message);
