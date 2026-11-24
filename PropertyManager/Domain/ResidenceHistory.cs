namespace PropertyManager.Domain;

public record ResidenceHistory(Guid ApplicationId, Guid UserId, Address Address, string LandlordName, DateOnly MoveInDate, DateOnly MoveOutDate);