namespace PropertyManager.Data.Seeds;

public interface ISeeder
{
    Task Seed(CancellationToken cancellationToken = default);
}
