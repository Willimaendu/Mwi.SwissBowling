# First championship workflow

## Scope and architecture

.NET 10, one feature-sliced Minimal API, EF Core/PostgreSQL, Blazor SSR and WebAssembly, a BFF, Keycloak, OpenTelemetry and an Aspire-only local workflow. Modern Clubhouse uses forest green, warm ivory and restrained copper, with accessible keyboard-first administration. Core owns operational concerns; API features own business rules. Contracts are browser-safe DTOs, never EF entities. Map explicitly; do not add mapper libraries.

Use Directory.Build.props and NuGet Central Package Management. Only freely usable dependencies are allowed; review transitive licenses and production restrictions. Do not assume the repository itself has a license before one is chosen.

## Acceptance criteria

1. A registered user starts pending. Only an approved account with a trusted Keycloak administrator role may use administrative API endpoints. Public standings need no account. Player/club self-service is a later slice.
2. Initial administrator creation is an explicit command, not public registration or an implicit production seed. Account approval and player linking are audited. Issuer plus immutable subject identifies actors; email and SFKV number are not identity proofs.
3. An administrator can maintain minimal seasons, clubs, players, season assignments, venues with numbered lanes, annual championship containers and events. Members have a unique SFKV number; guests have only an internal identifier. Date of birth and contact data remain private. No fictitious production seed or unsupported MAP importer is allowed.
4. One result exists per player/event. Capture non-negative integer total, 9s, 8s and whether play was interrupted. The known hit counts cannot exceed or contradict the total. An optional event throw limit bounds the score; do not assume 100 throws. Preserve partial results from interrupted play. The first slice does not calculate promotion/relegation.
5. Exclusive event/player editing leases live in PostgreSQL. Default expiry is five minutes, renewed every 30 seconds while editing. No forced takeover. Save checks actor, token, expiry and expected version. Expired editors cannot overwrite newer results. Do not hold a transaction open during human editing.
6. Separate A/B/C/guest standings sort by total, 9s, 8s descending; exact ties share ranks with gaps. This deliberately stops earlier than the full Article 42 sequence. Event ranking needs no lower hit counts in this slice.
7. Working results are private. Finalization requires at least one valid result and publishes a versioned, immutable public projection atomically. Birth dates, contacts, drafts and identity metadata never enter it.
8. Event states: Draft -> Published; a request alone does not reopen it. A different approved administrator approves a pending correction request, moving Published -> Correcting. The proposer cannot approve their own request even as system administrator. Republish moves Correcting -> Published. While Correcting, public pages show the last snapshot with `In Korrektur`. Reopening requires a reason. Audit sporting changes permanently without storing player contact snapshots.
9. Public HTML is server rendered, indexable and shareable without WebAssembly. Updates appear on page load/manual refresh. API responses containing private data are not publicly cached. Client-side hiding is never an authorization control.
10. BFF tokens stay server-side. Protect cookies, apply antiforgery to browser mutation endpoints, use verified OIDC/PKCE and administrator MFA. Share encrypted authentication tickets and certificate-protected Data Protection keys across web replicas. Configure certificates/secrets externally; fail closed when missing.
11. Unit tests cover scoring and transitions; PostgreSQL-backed integration tests cover constraints, competing leases, stale saves and publication privacy; browser tests cover critical journeys and permission denials. A passing build is not a 1,000-visitor capacity claim.
12. Schema changes run as explicit versioned deployment commands, not on each app replica startup. GitHub Actions validates builds/tests/containers and requires manual release approval. Production platform, license, email provider, recovery objectives and retention of personal data remain open.

## Minimal endpoint contract

- `/api/identity/me`, `/register`, `/pending`, `/{id}/approve`: authenticated profile, pending registration and administrator approval.
- `/api/setup/*`: approved-administrator setup/read operations. Public routes never reuse private DTOs.
- `/api/events/{id}/results`, `/standings`: internal editing/review.
- `/api/events/{id}/leases/{playerId}`: acquire, renew and release; a lease token is not a substitute for authenticated ownership.
- `/api/events/{id}/finalize`, `/corrections`, `/corrections/{requestId}/approve`: controlled publication.
- `/api/public/events`, `/api/public/events/{id}`: published projections only, including correction status.

Return RFC Problem Details; use 400 for invalid input, 401/403 for access denial, 404 for missing resources and 409 for lock/version/state conflicts. Do not expose database exceptions.

## Deferred functionality and unresolved rules

Annual and club scoring, cups, MAP import, PDF/CSV exports and player/club change-request portals follow later. MAP has no verified public export schema. Obtain supported documentation from the Unterverband IT representative. Clarify local Article 31 average-result eligibility, club tie-break interpretation, score-rounding midpoint behavior and promotion scope before implementing those features. Hosting and restore objectives need provider-specific decisions.
