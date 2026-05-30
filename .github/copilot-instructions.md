# GitHub Copilot Instructions for eShop

## Build, Test, and Run

```bash
# Build the solution
dotnet build eShop.Web.slnf

# Run all tests
dotnet test --solution eShop.Web.slnf --no-build --no-progress --output detailed

# Run a single test project
dotnet test tests/Ordering.UnitTests/ --no-build

# Run a single test by name filter
dotnet test tests/Ordering.UnitTests/ --filter "FullyQualifiedName~CreateOrder"

# Run the application (requires Docker)
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

> Functional tests (`Catalog.FunctionalTests`, `Ordering.FunctionalTests`) spin up Docker containers via Aspire — Docker must be running.

## Architecture Overview

This is a .NET 9 microservices e-commerce reference app orchestrated with **.NET Aspire** (`src/eShop.AppHost`). The AppHost wires together all services, databases, and message brokers using the Aspire resource model.

**Services:**
- `Catalog.API` — Minimal API, pgvector semantic search, API versioning (v1/v2)
- `Basket.API` — gRPC service, backed by Redis
- `Ordering.API` — CQRS with MediatR, DDD aggregates
- `Identity.API` — Duende IdentityServer (OAuth2/OIDC)
- `OrderProcessor` / `PaymentProcessor` — Background worker services
- `Webhooks.API` — Webhook subscription and delivery
- `WebApp` — Razor Pages frontend with Tailwind CSS
- `HybridApp` — .NET MAUI cross-platform client

**Infrastructure:**
- PostgreSQL with pgvector (Catalog, Identity, Ordering, Webhooks databases)
- Redis (basket cache)
- RabbitMQ (integration event bus)
- YARP reverse proxy (`mobile-bff`) for the mobile/MAUI client

## Key Conventions

### Service Bootstrap Pattern
Every service calls `builder.AddServiceDefaults()` from `eShop.ServiceDefaults`, which configures OpenTelemetry (traces/metrics/logs), HTTP resilience with Polly, service discovery, and health check endpoints (`/health`, `/alive`).

### CQRS in Ordering.API
`Ordering.API` uses MediatR with a layered CQRS pattern:
- `Application/Commands/` — command + handler pairs (e.g., `CreateOrderCommand` / `CreateOrderCommandHandler`)
- `Application/Queries/` — queries using EF Core directly (no MediatR)
- `Application/Behaviors/` — cross-cutting MediatR pipeline behaviors: `LoggingBehavior`, `TransactionBehavior`, `ValidatorBehavior`
- `Application/DomainEventHandlers/` — respond to domain events published by aggregates
- Idempotency is handled via `IdentifiedCommand<T>` wrapping

### DDD in Ordering.Domain
Domain entities inherit from `Entity` (which carries `IReadOnlyCollection<INotification> DomainEvents` and dispatches them through EF via `MediatorExtension.DispatchDomainEventsAsync`). Aggregates implement `IAggregateRoot`. Value objects inherit `ValueObject`. The repository pattern uses `IRepository<T>` backed by EF Core in `Ordering.Infrastructure`.

### Integration Events
Cross-service communication uses an `IEventBus` abstraction (`src/EventBus`), implemented over RabbitMQ (`src/EventBusRabbitMQ`). Published events use a transactional outbox pattern via `IntegrationEventLogEF`. Each service defines its own integration event types under `IntegrationEvents/`.

### API Style
- `Catalog.API` uses **Minimal APIs** with `MapCatalogApi()` extension, supporting API versioning (`HasApiVersion(1,0)` / `HasApiVersion(2,0)`).
- `Ordering.API` uses MVC controllers delegating to MediatR commands/queries.
- All APIs use `AddProblemDetails()` and `MapDefaultEndpoints()`.

### Functional Test Pattern
Functional test fixtures (e.g., `CatalogApiFixture`) extend `WebApplicationFactory<Program>` and embed an Aspire `DistributedApplication` to provision real containers. Tested projects expose `public partial class Program { }` in a `Program.Testing.cs` file.

### Package Management
All NuGet package versions are centrally managed in `Directory.Build.props` → `Directory.Packages.props`. Do not specify versions in individual `.csproj` files. Global properties include `TreatWarningsAsErrors=true` and `ImplicitUsings=enable`.

### Namespaces
Follow the pattern `eShop.<ServiceName>.<Layer>`, e.g., `eShop.Catalog.API`, `eShop.Ordering.Domain.Seedwork`, `eShop.Ordering.Infrastructure`.

### AI / OpenAI Integration
OpenAI and Ollama integrations are opt-in. Toggle `bool useOpenAI = false` or `bool useOllama = false` in `src/eShop.AppHost/Program.cs`. For Azure OpenAI, add a `ConnectionStrings:OpenAi` entry in `appsettings.json`.
