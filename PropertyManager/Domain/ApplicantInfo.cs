namespace PropertyManager.Domain;

public sealed record ApplicantInfo
{
    private ApplicantInfo()
    {
    }

    public ApplicantInfo(
        Guid UserId,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email,
        Address CurrentAddress)
    {
        this.UserId = UserId;
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.PhoneNumber = PhoneNumber;
        this.Email = Email;
        this.CurrentAddress = CurrentAddress;
    }

    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Address CurrentAddress { get; private set; } = null!;
}
