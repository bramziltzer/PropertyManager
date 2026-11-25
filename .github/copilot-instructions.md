# Copilot instructions

## Project overview

- This repository contains a .NET 10 ASP.NET Core MVC property-management website.
- The application is organized into:
  - `PropertyManager/Controllers`: MVC and HTTP concerns.
  - `PropertyManager/AppService`: application services that coordinate use cases.
  - `PropertyManager/Domain`: entities, domain behavior, rules, and domain exceptions.
  - `PropertyManager/Data`: EF Core `ApplicationDbContext`, entity configurations, migrations, and seeders.
  - `PropertyManager/Models`: presentation/view models.
  - `PropertyManager/Views`: Razor views.

## Architecture rules

- Keep controllers thin. A controller action should validate/shape HTTP input, call an application-service interface, and return a view, redirect, or HTTP result.
- Do not inject `ApplicationDbContext`, use `DbSet`, run LINQ against EF Core entities, or perform queries/persistence directly in controllers.
- Put fetching, persistence, save operations, and transaction orchestration in `AppService` implementations. Controllers and Razor views should not contain data-access logic.
- Application services coordinate application use cases: load the required data, invoke domain methods, and persist changes. Do not duplicate domain rules in controllers or services when the behavior belongs on a domain entity.
- Keep business invariants and state transitions in `Domain`. Prefer domain methods such as entity operations over directly changing domain state from a controller.
- Prefer injecting application-service interfaces (for example, `IRentalApplicationService`) into controllers and register implementations in dependency injection.
- Keep EF Core mapping in `Data/Configurations`; do not put persistence annotations or EF-specific behavior in domain models when a fluent configuration can be used.
- Keep Razor page/authentication behavior in its existing areas unless the change specifically concerns MVC controllers.

## C# and ASP.NET Core conventions

- Use file-scoped namespaces and follow the existing nullable-reference setup.
- Use constructor or primary-constructor dependency injection.
- Use async/await with cancellation support for application-service and EF Core I/O when appropriate.
- Return focused request/response or view-model types rather than passing EF entities through controller responses merely for convenience.
- Handle known domain exceptions explicitly at the application/HTTP boundary rather than swallowing them or returning success-shaped results.
- Follow existing naming conventions and keep public members and domain behavior clear enough to read without explanatory comments.
- Add XML documentation only where it communicates a contract or important exception behavior, as is already done for domain operations.
- Preserve the existing .NET 10 target and do not introduce a second architecture, CQRS layer, or repository wrapper unless the task requires one.

## Validation and changes

- Add focused tests for new domain rules and application-service behavior when a test project is available.
- When changing persisted models, update the EF Core configuration and create an appropriate migration.
- Do not directly edit generated EF Core migration snapshot/designer files except through the migration tooling.
- Run the narrowest relevant `dotnet build` or `dotnet test` command after code changes and address failures caused by the change.
