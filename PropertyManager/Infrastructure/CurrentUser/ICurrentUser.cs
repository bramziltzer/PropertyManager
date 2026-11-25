namespace PropertyManager.Infrastructure.CurrentUser;

public interface ICurrentUser
{
    Guid Id { get; }

    bool IsPropertyManager { get; }
}
