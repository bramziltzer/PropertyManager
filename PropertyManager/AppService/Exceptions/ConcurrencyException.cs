namespace PropertyManager.AppService.Exceptions;

public sealed class ConcurrencyException(
    string message = "The record was modified by another user. Please reload and try again.",
    Exception? innerException = null)
    : Exception(message, innerException);
