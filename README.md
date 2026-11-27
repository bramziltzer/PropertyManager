# PropertyManager

## Prerequisites

- Docker Desktop (recommended) to run the application and SQL Server
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) only for running tests or the app outside Docker

## Quick start

From the repository root:

```bash
docker compose up --build 
```

Or to run in the background (headless mode):

```bash
docker compose up --build -d
```

The app applies migrations and seeds the database
on startup.

Open `http://localhost:8080` to view the application.

Stop and remove the containers with `docker compose down`.

To reset the demo database and all seeded data:

```bash
docker compose down -v
docker compose up --build
```

For a local SQL Server instance, configure
`ConnectionStrings:DefaultConnection` in `PropertyManager/appsettings.json`, or
override it with `ConnectionStrings__DefaultConnection` when running the app
with `dotnet run`.

The `sa` password in `appsettings.json` and `docker-compose.yaml` is a
local-development default for the assessment. Change it before using this setup
outside local development.

## Seeded accounts

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

## Test

From the repository root, restore dependencies and run the test project:

```bash
dotnet restore PropertyManager.Tests/PropertyManager.Tests.csproj
dotnet test PropertyManager.Tests/PropertyManager.Tests.csproj
```

To run tests without restoring first:

```bash
dotnet test PropertyManager.Tests/PropertyManager.Tests.csproj --no-restore
```
