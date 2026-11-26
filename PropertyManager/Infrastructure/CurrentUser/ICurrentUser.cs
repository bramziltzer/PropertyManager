namespace PropertyManager.Infrastructure.CurrentUser;

public interface ICurrentUser
{
    Guid Id { get; }

    bool IsApplicant { get; }

    bool IsPropertyManager { get; }
}
