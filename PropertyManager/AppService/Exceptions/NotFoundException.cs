namespace PropertyManager.AppService.Exceptions;

public sealed class NotFoundException(string message = "The requested resource was not found.")
    : Exception(message);
