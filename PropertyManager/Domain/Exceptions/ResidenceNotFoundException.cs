namespace PropertyManager.Domain.Exceptions;

public class ResidenceNotFoundException(Guid residenceHistoryId)
    : DomainException($"Residence '{residenceHistoryId}' does not belong to this application.");
