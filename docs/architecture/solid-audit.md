# SOLID audit and refactoring

## Design policy

Apply SOLID throughout feature development and review. Prefer cohesive responsibilities and explicit boundaries over adding interfaces or inheritance mechanically. Keep feature-local business rules in their feature, operational infrastructure in Core, and browser-safe records in Contracts. Do not introduce generic repositories around EF Core, mapper libraries, or speculative extension frameworks.

- **Single responsibility:** distinguish pure policy, application orchestration, persistence and presentation when they have independent reasons to change.
- **Open/closed:** use existing extension mechanisms (DI registration, authorization handlers and EF configurations). Extract strategies only when genuinely variable behavior requires them.
- **Liskov substitution:** implementations must preserve cancellation, error and security semantics of their contracts. Do not use subclasses that weaken ownership or approval checks.
- **Interface segregation:** keep contracts focused on their consumers. Split a contract when real clients need independent capabilities, not solely to increase interface count.
- **Dependency inversion:** presentation depends on explicit transport/application boundaries. Select concrete implementations at composition roots. Framework abstractions such as HttpClient handlers, TimeProvider and EF Core already provide useful seams.

## Completed first pass

| Area | Change | Preserved behavior |
| --- | --- | --- |
| Result recording | Extracted EditingLeaseRules from ResultRecordingService | Immutable issuer/subject plus token ownership, exact expiry boundary, existing event/season locks and transactions |
| Web BFF | Extracted BffRequestPolicy from forwarding endpoints | Feature-root allowlist, local redirect rules, authentication and antiforgery enforcement order |
| Browser transport | Extracted ApiResponseErrors from AdminApi | Status codes, safe authentication errors, validation messages and malformed-response fallback |
| Administration UI | Components depend on IAdminApi; implementation selected in WASM Program | Same BFF routes, CSRF acquisition and HTTP operations |

Direct rule/policy tests complement the existing transport, SSR, scoring, standings and publication tests. These changes do not alter schema, public DTOs or deployment behavior.

## Existing cohesive boundaries retained

ScoreRules, StandingRules, PublicationRules and SetupValidation already isolate pure decisions. Publication snapshot creation and result saves intentionally retain transactional orchestration with EF Core. Core ticket persistence, schema deployment, trusted role transformation, transport records and Aspire resource composition do not require one interface per class merely to satisfy a checklist.

## Codebase-wide follow-up

| Area | Responsibility boundary | Preserved behavior |
| --- | --- | --- |
| Setup API | Route mapping, SetupCommands and SetupQueries | Validation, audit ownership, roster locks and projections |
| Identity API | Route mapping and IdentityOperations | Approval policy, immutable identity and audited registration/approval |
| Publication | PublicationService commands and PublicationQueries read models | Atomic publication/correction and snapshot-only public reads |
| Core hosting | Operational registration, database readiness and safe exception handling | Readiness scope, safe error responses and telemetry |
| Web authentication | WebSessionValidator and IdentityTokenRefresher | Serialized database refresh, bounded cancellation and server-side tokens |
| Public pages | PublicEventsClient and SSR presentation | Public API routes and useful 404/503 HTML |
| Result editor | EditingLeaseSession and score-entry presentation | Renewal cancellation, ownership invalidation, input retention and no automatic write retries |
| Setup UI | Seven focused forms and workspace execution feedback | Existing field labels/IDs, API payloads and parent refresh |

EditingLeaseSession uses TimeProvider for renewal timing. Lifecycle regression tests cover release cancellation, expired ownership, unconfirmed release and renewal failure. SetupFormBase shares only the form execution contract, data and optional-value normalization; each form owns its own inputs and payload construction.

Removed the historical static UI comparison prototype and the empty BrowserTests project, including its solution entry and unused Playwright dependency. Production assets, explicit SQL migrations, deployment configuration and implemented tests remain necessary and are retained. Removing an empty project does not constitute browser-test coverage.

This is not a certification of subjective SOLID perfection. IAdminApi remains a low-level transport contract; introduce typed feature clients only when real workflow consumers justify them, not as speculative abstractions. Transactional commands intentionally continue to use EF Core directly.

Database locking/atomicity regression tests still require real PostgreSQL in CI because Docker is unavailable locally. Unit tests do not replace those guarantees or live browser/Keycloak validation.
