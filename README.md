# PropertyManager

## Development database

The application applies EF Core migrations and seeds development data on startup.
Configure the SQL Server connection in `PropertyManager/appsettings.json` or with
the `ConnectionStrings__DefaultConnection` environment variable.

Seeded accounts:

| Role | Email | Password |
| --- | --- | --- |
| Property Manager | `manager@propertymanager.test` | `PropertyManager1!` |
| Applicant | `ada@propertymanager.test` | `PropertyManager1!` |
| Applicant | `grace@propertymanager.test` | `PropertyManager1!` |
| Applicant | `linus@propertymanager.test` | `PropertyManager1!` |
| Applicant | `katherine@propertymanager.test` | `PropertyManager1!` |
| Applicant | `margaret@propertymanager.test` | `PropertyManager1!` |
| Applicant | `alan@propertymanager.test` | `PropertyManager1!` |

The seed is idempotent and includes properties, units, an inactive unit type on an
existing unit, every application status, and an active lease issued from an
approved application.

## Run and test

```bash
dotnet run --project PropertyManager/PropertyManager.csproj
dotnet test PropertyManager.Tests/PropertyManager.Tests.csproj
```
