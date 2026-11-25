namespace PropertyManager.Domain;

public sealed record ApplicantInfo
{
    private ApplicantInfo()
    {
    }

    public ApplicantInfo(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string Email,
        Address CurrentAddress)
    {
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.PhoneNumber = PhoneNumber;
        this.Email = Email;
        this.CurrentAddress = CurrentAddress;
    }

    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Address CurrentAddress { get; private set; } = null!;
}
