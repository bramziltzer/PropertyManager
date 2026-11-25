namespace PropertyManager.Domain;

public sealed record ApplicantInfo(
    string UserId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string Email,
    Address CurrentAddress);
