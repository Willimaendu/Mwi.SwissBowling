# Administration modernization

## Confirmed scope

Modernize all existing administration workflows; do not introduce new annual/club calculations or import/export. Push updates to administration and public standings without manual refresh. Retain .NET 10, feature slices, Keycloak, PostgreSQL, Blazor SSR/WebAssembly and the server-side-token BFF. Preserve existing data, certificates and local changes.

## UI

Use themed MudBlazor with forest green, ivory and restrained copper. Separate overview, result entry, master data and approvals. Use searchable, sortable, paginated grids for lists; use explicit forms and keyboard-first numeric inputs for score entry. Show loading, empty, failed, unauthorized and reconnecting states separately. Never overwrite unsaved score input during synchronization. Preserve editing leases, optimistic concurrency and two-person correction approval.

## Live update architecture

PostgreSQL statement triggers issue payload-free administration invalidations on committed domain writes. Public invalidations are restricted to publication snapshots and changes in the state of already-published events. Transaction rollback produces no notification. Each Web replica maintains a dedicated LISTEN connection and fans out through its local SignalR hub. No sticky sessions or in-memory cross-replica state are required when the browser uses WebSockets directly (without negotiation). The hosting proxy must support WebSockets.

The public hub accepts anonymous connections; the administration hub requires the existing cookie/BFF policy and closes connections on authentication expiration. The hubs expose no mutation methods, domain records, identifiers, tokens or secrets. Enforce same-origin browser connections. Clients reload data through existing authorized HTTP endpoints after notifications and after reconnect. Notifications are hints, not durable events: PostgreSQL listener reconnection also invalidates every connected client. Coalesce reloads, cancel on disposal and preserve unsaved form state. If reliable business-event delivery becomes necessary, use a transactional outbox rather than treating NOTIFY as a durable queue.

The Web host needs a separate `ConnectionStrings:notifications` database connection to the application database. Aspire supplies the application resource connection locally. Production should provision a dedicated least-privilege role with CONNECT only (no table read/write rights) for LISTEN; do not grant the Web host application table access. This supplements, rather than replaces, the existing webstate connection.

Public pages retain SSR metadata and initial content. An interactive public component receives updates and reloads only published data. Draft results must never enter the public hub or public API.

## Delivery / acceptance gates

- Diagnose the actual login redirect chain. An initial 302 challenge is expected, not evidence of a defect. Verify OTP, callback, cookie/session persistence, authorization and logout with live infrastructure.
- Deliver the live-update spine and connection/reconnection behavior, with transactional PostgreSQL integration tests.
- Modernize administration shell, master-data lists/forms, approvals and result entry using MudBlazor.
- Replace manual refresh workflows with automatic synchronization and visible connection status.
- Validate builds/unit tests, PostgreSQL integration tests, two-browser updates, multi-replica fanout, expired sessions, network interruption, keyboard navigation and responsive layout.

A passing unit suite is not proof of live Keycloak login or browser usability. Record unverified gates explicitly.
