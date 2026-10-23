# Dependency and release policy

- Target .NET 10 across production and test projects. `global.json`, `Directory.Build.props` and `Directory.Packages.props` are authoritative. Keep package versions out of project files.
- Use freely available packages. No mapper library: use explicit feature-local mappings and projections. New dependencies require a maintenance, security, licensing and deployment review.
- Restore audits direct and transitive packages. Do not suppress vulnerability warnings simply to pass a build. Investigate advisories and upgrade compatible dependencies.
- The current direct dependency families (Microsoft .NET/Aspire, MudBlazor, OpenTelemetry, Npgsql, xUnit and Testcontainers) have open-source distributions. This is not a completed legal review: retain license notices and inspect the licenses of exact resolved transitive dependencies and container operating-system packages before distribution. Npgsql/EF version compatibility must be reviewed with each upgrade.
- No project license has been selected. Do not infer permission to redistribute this repository from dependency licenses. Docker Desktop and managed hosting are separate operational products; a free library does not imply a free service or tooling subscription.

## Renovate

`.github/renovate.json` uses native NuGet/central package management, global.json, Dockerfile and GitHub Actions detection. Routine pull requests are scheduled weekly in Europe/Zurich; vulnerability alerts bypass that schedule and release-age delay. No automatic merges. Major NuGet upgrades require dashboard approval. Review provider compatibility, migrations and browser behavior, not only compilation.

Enable the Renovate GitHub App or an equivalent controlled self-hosted instance. Run the current Renovate config validator before enabling it and after configuration changes. The configuration previously passed the Renovate 44.179.0 validator. Both workflows pin checkout and setup-dotnet to verified full commits, and both Dockerfiles pin .NET base-image manifest digests. Review Renovate pin-update PRs rather than substituting unverified hashes. JSON parsing alone does not replace Renovate schema validation.

## CI and releases

CI restores, builds, tests against real PostgreSQL in Docker, and builds both container images. GitHub-hosted Linux runners provide Docker; local development remains Aspire-only. Workflows do not run on `pull_request_target` and untrusted PRs receive no publishing credentials. The dependency inventory command produces the exact resolved direct/transitive package list; it is not an SBOM or a license audit. TRX results and the dependency inventory are uploaded as a verification artifact retained for 14 days, including after failed steps when files exist. Treat test output as potentially sensitive; do not log credentials or real personal data.

When a local container engine is unavailable, run builds and the unit-test project locally. Push a branch and open a pull request, or select **Actions → CI → Run workflow** for that branch when the workflow is available on the default branch. Inspect the Docker availability check, PostgreSQL integration tests and both image-build steps, then download the verification artifact for TRX details. Independent checks continue after test failures, but failures still fail the job; no integration test is replaced with an in-memory substitute. A Docker availability failure is an infrastructure failure, not a passing integration run. GitHub Actions availability, runner usage limits and repository settings must be confirmed separately. This workflow does not yet verify live Keycloak or browser journeys.

Container publishing is manually dispatched on `main`, repeats build/tests, and uses source-revision tags without a mutable `latest` tag. Configure the GitHub `release` environment with required reviewers, prevent self-review, and restrict it to `main` **before enabling publishing**. Environment protection is repository configuration, not enforceable by YAML alone. Require CI and review through branch protection. Deploy by registry digest; source-revision tags can still be overwritten by registry users.

Publishing does not deploy or migrate a database. Promotion, signature/attestation verification, vulnerability scans, hosting credentials, rollback approval, and the production provider remain deployment prerequisites. Never expose bootstrap/seed commands as HTTP endpoints or run them automatically in production.
