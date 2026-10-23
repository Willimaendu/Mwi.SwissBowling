# SwissBowling

.NET 10 championship workflow for Swiss bowling (Willisau-Sursee). A feature-sliced REST API, PostgreSQL, a Blazor SSR public site and a Blazor WebAssembly administration workspace behind a server-side BFF. Authentication is provided by Keycloak; tokens never need to be stored by browser application code.

The first workflow includes setup, seasonal player/club assignments, versioned aggregate result entry with shared editing leases, category rankings, immutable public snapshots, and two-person correction approval. Annual/club calculations, cups, import/export, and full player self-service are deferred. Domain scope and acceptance criteria: [first workflow](docs/architecture/first-workflow.md).

## Local development (Aspire only)

Prerequisites: SDK selected by `global.json`, an Aspire-capable Visual Studio or SDK environment, and a Linux Docker-compatible engine (for example Rancher Desktop). PostgreSQL 18, Keycloak and Mailpit run as Aspire resources. No Docker Compose setup is required.

From the repository root:

1. Run `powershell -ExecutionPolicy Bypass -File scripts/Initialize-Development.ps1` once. It stores random development secrets in AppHost user secrets and creates a Data Protection PFX outside the repository. Do not rerun against existing persisted session keys or replace the certificate blindly.
2. Run `dotnet dev-certs https --trust`.
3. Run `dotnet run --project src/Mwi.SwissBowling.AppHost` (or choose AppHost as the Visual Studio startup project).
4. Use the Aspire dashboard to inspect resource health. One-off API and web schema jobs complete before their application resources start.

Development endpoints: public web `https://localhost:7443`, API `https://localhost:7444`, Keycloak `http://localhost:8080`, Mailpit `http://localhost:8025`. Use the actual dashboard endpoint if a launch profile changes these addresses.

The realm imports two fictitious administrative identities, `admin@swissbowling.local` and `sport@swissbowling.local`; retrieve their shared development password privately from AppHost user secrets (`Parameters:development-password`). Both require OTP enrollment. Keycloak bootstrap credentials and application approval are separate. Never publish user-secret output or reuse development identities/configuration in production.

Application administrator bootstrap is explicit: run the API deployment command `--bootstrap-admin <immutable-subject>` with the same `ConnectionStrings__application` and `Authentication__Authority` as the running API. The development admin subject is `11111111-1111-4111-8111-111111111111`; the sport administrator subject is `22222222-2222-4222-8222-222222222222`. Supply database credentials through a private shell/secret store, not tracked launch settings. Verify the command implementation and target database before executing. Administrative Keycloak roles alone do not bypass application-account approval.

## Verification

- `dotnet restore Mwi.SwissBowling.slnx`
- `dotnet build Mwi.SwissBowling.slnx`
- `dotnet test tests/Mwi.SwissBowling.UnitTests`
- `dotnet test tests/Mwi.SwissBowling.IntegrationTests` (requires Docker; uses isolated real PostgreSQL containers)

Live Keycloak OTP/login/logout/token refresh, multiple web replicas, browser accessibility, container startup and production capacity are separate checks, not established by a successful unit test run. See [deployment checklist](docs/architecture/deployment.md) and [dependency policy](docs/architecture/dependency-policy.md).

## Containers

Build with the repository root as context:

- `docker build -f src/Mwi.SwissBowling.Api/Dockerfile -t swissbowling-api:local .`
- `docker build -f src/Mwi.SwissBowling.Web/Dockerfile -t swissbowling-web:local .`

Images run as the .NET non-root application user on port 8080. No secrets or private certificate files are copied. They require externally configured PostgreSQL, identity, certificates, routing and HTTPS. These Dockerfiles package deployable artifacts; they do not replace Aspire for local orchestration.
