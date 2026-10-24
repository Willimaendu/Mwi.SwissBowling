# UI component architecture

SwissBowling uses an Aspire Dashboard-inspired shell with themed MudBlazor, retaining the Modern Clubhouse forest-green, warm-ivory and copper palette. Aspire is a design/organization reference; the application does not embed its dashboard or copy its services.

## File convention

- `Component.razor`: markup, binding expressions, route/render directives and declarative UI conditions. No `@code` blocks or service injection directives.
- `Component.razor.cs`: file-scoped namespace and a public partial class, with explicit C# imports, injected properties, parameters, lifecycle methods, state and event handlers.
- `Component.razor.css`: component-specific responsive and print styles. Anchor `::deep` selectors to a component-owned HTML wrapper when styling a child component.
- Keep markup-only composition files such as App and Routes free of empty code-behind files. Provider composition needs no empty stylesheet when it renders no owned visual surface.
- Setup forms retain `@inherits SetupFormBase` so generated markup and code-behind agree on their base class.

Use focused services for application behavior; do not move HTTP contracts, persistence or authorization rules into markup/code-behind merely because it is convenient.

## Layout and styles

`MainLayout` owns the application header, persistent desktop navigation rail, native keyboard-accessible mobile navigation, content region and footer. Administration owns a second, contextual workflow navigation beside its content. Native navigation and result inputs remain keyboard-accessible without JavaScript-only menus.

`wwwroot/app.css` contains shared tokens, typography, native-control styling and reusable small presentation primitives. `modern-clubhouse.css` contains cross-cutting MudBlazor adjustments only. Feature and shell layout rules live beside their components.

`App.razor` loads `Mwi.SwissBowling.Web.styles.css`, the host's generated isolated-CSS bundle. The Blazor build composes referenced client-component scoped CSS into that bundle; do not link generated intermediate files or the client bundle separately.

## Rendering and security boundaries

Public pages retain server-rendered metadata, initial public data and meaningful 404/503 status codes. PublishedEvents and PublishedRanking are prerendered WebAssembly islands. Administration remains a non-prerendered WebAssembly island behind the existing cookie BFF. Theme/popover/dialog/snackbar providers belong inside each interactive root rather than a static server layout.

The refactor does not change .NET 10, Keycloak login, server-side tokens, CSRF protection, API authorization, PostgreSQL notifications, SignalR reconnect behavior, editing leases or optimistic concurrency. Do not move secrets or bearer tokens to the browser. Runtime login and multi-browser update checks remain separate acceptance gates from static rendering tests.

## Validation

Run the solution build and unit tests after component moves. Rendering tests cover published rankings, empty/outage/unauthorized states, shell navigation, contextual headings and the authenticated administration overview. Validate the generated CSS bundle and responsive layout in a real browser on a machine with the required infrastructure. PostgreSQL integration tests require a working container engine.
