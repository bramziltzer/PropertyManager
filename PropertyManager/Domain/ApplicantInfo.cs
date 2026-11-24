using Microsoft.AspNetCore.Identity;

namespace PropertyManager.Domain;

public record ApplicantInfo(Guid UserId, string FirstName, string LastName, string Email, string PhoneNumber, Address Address);
