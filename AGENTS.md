# eShop Agent Guide

.NET 10 reference e-commerce app built on .NET Aspire. Multi-service solution orchestrated by `src/eShop.AppHost` (the only project you run to start the whole stack). This file only documents things that are not obvious from the directory tree or that an agent is likely to get wrong.

## Solutions and projects

- `eShop.slnx` — full solution (includes `ClientApp` MAUI + `HybridApp` and their tests).
- `eShop.Web.slnf` — solution filter used by CI for the PR build. **Use this for almost everything**; it omits MAUI projects. `eShop.AppHost.csproj` is the startup project.
- All `*.csproj` under `src/` and `tests/` target `net10.0`; AppHost and service SDK is `Aspire.AppHost.Sdk/13.3.5`.
- `Directory.Packages.props` centrally pins every package version. **Do not add `<Version>` on `<PackageReference>` in a child csproj**; add or update a `PackageVersion` entry in `Directory.Packages.props` instead.
- `Directory.Build.props` sets `TreatWarningsAsWarnings=true` (effectively `TreatWarningsAsErrors`); new warnings break the build.

## Required toolchain

- .NET SDK `10.0.100` with `rollForward: latestFeature` and `allowPrerelease: true` — see `global.json`. `dotnet --version` must satisfy this, or restore/build fails before doing anything.
- Docker Desktop running and started (Redis, RabbitMQ, and Postgres+pgvector containers are launched by Aspire at runtime).
- Node.js (LTS) and `npm ci` for the Playwright suite and the `WebApp` front-end build (Vite + Tailwind).
- For `ClientApp` / `HybridApp` (MAUI): `dotnet workload install maui android ios maccatalyst` on Windows. These are excluded from `eShop.Web.slnf`; only build them when you actually need them.

## Common commands

Run from repo root unless noted.

- Start the whole stack (Asks for Aspire dashboard on `http://localhost:19888`):
  ```
  dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
  ```
  First run needs the ASP.NET Core HTTPS dev cert: `dotnet dev-certs https --trust` and restart any open browsers.
- Build everything that is part of the PR validation:
  ```
  dotnet build eShop.Web.slnf
  ```
- Run all unit + functional tests (CI command):
  ```
  dotnet test --solution eShop.Web.slnf --no-build --no-progress --output detailed
  ```
- Run a single test project (faster feedback loop):
  ```
  dotnet test tests/Basket.UnitTests/Basket.UnitTests.csproj
  dotnet test tests/Ordering.UnitTests/Ordering.UnitTests.csproj
  dotnet test tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj
  dotnet test tests/Ordering.FunctionalTests/Ordering.FunctionalTests.csproj
  ```
  Functional tests (`*FunctionalTests`) **need Docker** — they spin up Postgres via `Aspire.Hosting` inside the test fixture.
- WebApp front-end assets (Vite/Tailwind). The MSBuild project runs these automatically, but for dev:
  ```
  cd src/WebApp && npm ci && npm run dev   # or `npm run build`
  ```
- Playwright e2e (requires the AppHost to be reachable on `http://localhost:5045`; `playwright.config.ts` starts it for you):
  ```
  npm ci
  npx playwright install chromium
  USERNAME1=bob PASSWORD='Pass123$' ESHOP_USE_HTTP_ENDPOINTS=1 npx playwright test
  ```

## Environment variables and toggles

- `ESHOP_USE_HTTP_ENDPOINTS=1` — switches the AppHost from HTTPS to HTTP launch profile and is required by Playwright in CI. Set this when running Playwright locally.
- `OTEL_EXPORTER_OTLP_ENDPOINT` — if set, `eShop.ServiceDefaults` switches OpenTelemetry exporters to OTLP.
- `USERNAME1` / `PASSWORD` — required by `e2e/login.setup.ts` (asserts at module load). The seeded dev user is `bob` / `Pass123$` (see `src/Identity.API/UsersSeed.cs`).
- OpenAI / Ollama: toggle `useOpenAI` / `useOllama` in `src/eShop.AppHost/Program.cs`. To use Azure OpenAI, add the `ConnectionStrings:OpenAi` value in `src/eShop.AppHost/appsettings.json` (the key is commented there as a template).

## Tech stack

- Runtime: **.NET 10** (`net10.0`), C# 14, Aspire 13.3.5 (distributed app orchestration).
- Web/API: ASP.NET Core minimal hosting, Razor Pages (`WebApp`), Blazor components (`WebAppComponents`), Web API endpoints in service projects.
- Service-to-service: gRPC (`Basket.API/Proto/basket.proto` consumed by `WebApp` and `ClientApp`), RabbitMQ via `EventBusRabbitMQ`, REST over HTTP for everything else.
- Datastores: PostgreSQL (with `ankane/pgvector` for catalog AI/embeddings) via EF Core + Npgsql, Redis for basket state, Dapper for some catalog/order queries.
- Auth: Duende IdentityServer (`Identity.API`, dev-licensed) with OIDC; `WebApp` and `MobileBff` (YARP reverse proxy) consume it.
- Observability: OpenTelemetry wired in `eShop.ServiceDefaults` (metrics, traces, logs); OTLP exporter toggled by `OTEL_EXPORTER_OTLP_ENDPOINT`.
- AI (optional, toggled in `AppHost/Program.cs`): Azure OpenAI or Ollama via `Aspire.Azure.AI.OpenAI` and `CommunityToolkit.Aspire.OllamaSharp`; `Pgvector` for embeddings.
- Front-end: Vite + Tailwind + Alpine.js + HTMX (built from `src/WebApp/package.json`).
- Mobile: .NET MAUI (`ClientApp`, `HybridApp`) with CommunityToolkit.Mvvm / .Maui.
- Testing: MSTest SDK 4.0.2 (unit + `ClientApp.UnitTests`) with NSubstitute; xunit.v3.mtp-v2 + `WebApplicationFactory<Program>` (functional); Playwright for e2e.

## Project layout worth knowing

- `src/eShop.AppHost` — Aspire distributed application; wires Redis, RabbitMQ, Postgres (with `ankane/pgvector`), and all services. **This is where new services get registered.**
- `src/eShop.ServiceDefaults` — shared `AddServiceDefaults()` / OpenTelemetry / health checks / service discovery used by every service via `builder.AddServiceDefaults()`.
- `src/Shared` — `ActivityExtensions.cs` and `MigrateDbContextExtensions.cs`, linked into services with `<Compile Include="..\Shared\..." Link="..." />` (see `Catalog.API.csproj`). Add new shared files the same way.
- `src/Basket.API` exposes a gRPC contract (`Proto/basket.proto`) consumed by `WebApp` and `ClientApp` via `<Protobuf Include=...>`.
- `src/Ordering.*` is split into `Domain`, `Infrastructure`, and `API`; tests reference all three.
- `src/Identity.API` uses Duende IdentityServer (free for dev). `tempkey.jwk` is a dev signing key, not a secret.
- `src/Catalog.API/Setup/catalog.json` is the source of truth for seed catalog data.

## Test quirks

- Unit test projects (`Basket.UnitTests`, `Ordering.UnitTests`, `ClientApp.UnitTests`) use the `MSTest.Sdk` with the `Microsoft.Testing.Platform` runner (configured in `global.json`). Output type is `Exe`; you run them as executables, not via VSTest.
- Functional tests (`Catalog.FunctionalTests`, `Ordering.FunctionalTests`) use `xunit.v3.mtp-v2` and inherit from `WebApplicationFactory<Program>`. This only works because `src/Catalog.API/Program.Testing.cs` and `src/Ordering.API/Program.Testing.cs` declare a public partial `Program` class — **do not remove those files**.
- `Ordering.FunctionalTests` also brings up an `Identity.API` resource and installs `AutoAuthorizeMiddleware` to bypass auth — keep that wiring intact when refactoring.
- Functional test fixtures call `IAsyncLifetime.InitializeAsync` to start the Aspire host and obtain a Postgres connection string at runtime; the tests therefore require Docker and are slow. Prefer unit tests for new coverage when feasible.
- `Basket.UnitTests/GlobalUsings.cs` sets `[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]` — test ordering assumptions must not rely on parallelism in this project.

## Coding conventions that bite

- `TreatWarningsAsErrors` is on. Fix warnings, do not silence them broadly.
- `.editorconfig` enforces `dotnet_sort_system_directives_first`, `csharp_new_line_before_open_brace = all`, var-preference, expression-bodied properties, etc. The default `dotnet new` style is not what's used here.
- C# files use 4-space indent; XML/MSBuild files use 2-space.
- `ImplicitUsings` and `Nullable` are enabled project-wide; new code is expected to be nullable-clean.
- `Shared/*.cs` files are linked, not project-referenced — when adding a shared helper, choose the most natural owning project or link it into each consumer; do not turn `Shared` into a class library without first updating every consumer's csproj.

## CI / workflows (`.github/workflows/`)

- `pr-validation.yml` (Ubuntu): `dotnet build eShop.Web.slnf` then `dotnet test --solution eShop.Web.slnf ...`. Paths to `src/ClientApp/**`, `tests/ClientApp.UnitTests/**`, and the MAUI workflow file are ignored here.
- `pr-validation-maui.yml` (Windows): builds/tests MAUI projects separately, requires `maui android ios maccatalyst` workloads.
- `playwright.yml` (Ubuntu): installs Playwright Chromium, trusts dev certs, then `npx playwright test` with `ESHOP_USE_HTTP_ENDPOINTS=1`, `USERNAME1=bob`, `PASSWORD=Pass123$`. Uploads `playwright-report/` on every run.
- `ci.yml` is the internal Azure DevOps pipeline (`1ESPipelineTemplates`) — not GitHub Actions, but it does `dotnet build eShop.Web.slnf`.

## Misc

- `nuget.config` clears all package sources and only allows `nuget.org`. If you add a private feed, do it here, not in `Directory.Packages.props`.
- `UseArtifactsOutput` is enabled; build output goes under `artifacts/` (gitignored).
- Generated OpenAPI docs (`*.API.json` / `*.API_v2.json`) are produced from the `Microsoft.Extensions.ApiDescription.Server` build target — do not hand-edit them.
- `global.json` pins the MSTest SDK version (`4.0.2`); bump it centrally if you need newer test features.
- There is no `AGENTS.md`, `CLAUDE.md`, or `.cursor/rules/` in the repo yet, so this file is the only agent-facing instruction set.
