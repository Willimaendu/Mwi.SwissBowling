# Deployment and operational checklist

## Configuration

Use secret injection rather than tracked files. The API needs `ConnectionStrings__application`, `Authentication__Authority` (the exact public realm issuer, HTTPS outside Development), and `Authentication__Audience=swissbowling-api`.

The web needs `ConnectionStrings__webstate`, `Api__BaseAddress` (explicit reachable URL outside Aspire), `Authentication__Authority`, `Authentication__ClientId`, `Authentication__ClientSecret`, `DataProtection__CertificatePath` and `DataProtection__CertificatePassword`. Mount the PFX read-only, accessible to the non-root user; do not put it in the image. Set `Site__PublicOrigin` to the canonical public HTTPS origin for link-preview metadata. Configure standard OTLP environment variables for a managed collector; protect collector credentials and keep personal data/tokens out of telemetry.

All web replicas must use the same webstate database, Data Protection application name and protecting certificate. Persist and back up keys as well as encrypted sessions. Retain old protecting certificates during rotation until old keys/tickets are no longer required. Loss of the certificate makes existing protected data unreadable. No session affinity is intended.

Keycloak production setup requires an HTTPS public hostname, persistent external database, confidential web client, exact HTTPS redirect/logout URIs, S256 PKCE, audience mapping, verified email and administrator MFA. The development `start-dev` realm/container is not a production deployment recipe. Explicitly review how role promotion revokes existing sessions and enforces OTP.

## HTTPS and network boundaries

The current applications use HTTPS redirection and do not configure trusted forwarded headers. For the supplied HTTP-port container images behind TLS termination, production must first configure ASP.NET Core forwarded headers with an explicit trusted proxy/network allowlist, or use end-to-end HTTPS with Kestrel certificate configuration. Do not simply trust arbitrary `X-Forwarded-*` headers. Without this step, redirect loops and incorrect OIDC callback URLs are possible. This is a deployment blocker, not an instruction to disable HTTPS.

Expose only web/identity and necessary API routes through authenticated infrastructure. Keep PostgreSQL, migration jobs, dashboards and telemetry ingestion private. Use managed database/application hosting where possible. Configure liveness `/alive` and database readiness `/health`; protect health endpoints from unnecessary external exposure. Readiness does not prove identity-provider availability.

## Schema, rollout and rollback

Run `dotnet Mwi.SwissBowling.Api.dll --migrate` with the target application connection string, then `dotnet Mwi.SwissBowling.Web.dll --migrate` with the target webstate connection string, as explicit deployment jobs. The embedded SQL runner serializes with an advisory transaction lock and verifies previously applied script checksums. Never edit an applied migration. Application replicas do not migrate on startup.

Use a separate migration database principal with DDL privileges. Runtime principals need only necessary table/sequence operations; do not grant schema ownership, trigger disabling or unrestricted DDL. Public snapshots and audit records have append-only triggers; privileges are still required for meaningful enforcement. Review exact grants against each migration before deployment.

Take an encrypted backup and verify restore before risky changes. Prefer backward-compatible expand/contract migrations so the old application can run during rollout. Application rollback does not reverse SQL or rewrite published snapshots. Establish and test RPO/RTO, retention, restore ownership and certificate recovery before production. None is selected yet.

Bootstrap the first application administrator once using the immutable issuer/subject and explicit command. Do not seed demonstration data in production. Configure GitHub environment review/branch protections as described in the dependency policy; container publishing itself is not production deployment.

## Release acceptance (must be exercised)

- Restore/build, dependency advisories/license review, unit tests and real PostgreSQL integration tests.
- Container builds, non-root startup, explicit migration jobs and persistence through restart.
- Public SSR ranking visible with JavaScript disabled; draft/private fields absent; last snapshot remains during corrections.
- Keyboard-only result entry, narrow viewport, screen-reader labels/status announcements and contrast review against WCAG 2.2 AA.
- Real Keycloak registration/email verification/OTP, approval, login/logout and expired session behavior.
- BFF CSRF rejection, API role/issuer/audience/signature denial, and lease ownership/version conflict across replicas.
- Refresh and encrypted tickets usable across web replicas, certificate rotation and database/certificate restore.
- Hosting-region/privacy/retention decisions, SMTP delivery, telemetry redaction and a representative load test. A 1,000-visitor goal is not a measured capacity claim.

As of implementation, live browser/OIDC journeys, container execution, recovery, production hosting and load remain unverified until the checklist is actually run. No project license or production provider has been selected.
