# Copilot Instructions

## Project Guidelines
- For the Swiss bowling application, use a modern maintainable secure .NET REST API and Blazor frontend, stateless cloud deployment, PostgreSQL, containers, local Docker Compose/Aspire, Keycloak authentication/authorization, OpenTelemetry, feature-slice architecture with cross-cutting project named Core, unit tests wherever possible and integration tests for critical functionality. Clarify domain requirements first, then architecture before implementation.
- Apply SOLID principles throughout SwissBowling code: focused responsibilities, explicit contracts, and dependency inversion where useful, without unnecessary abstraction.
- SwissBowling must target .NET 10 (net10.0), including its backend, Blazor projects, and test projects.
- Use file-scoped namespaces throughout SwissBowling C# code.

## SwissBowling UI Direction
- Rebuild SwissBowling using Microsoft Aspire Dashboard as the reference for appearance and code organization. Use Microsoft Fluent UI Blazor components, a neutral dashboard visual system, compact toolbars, persistent navigation and appropriate data grids; do not retain MudBlazor or the Modern Clubhouse theme.
- Separate component markup, behavior and scoped styles into .razor, .razor.cs and .razor.css files where applicable. Use file-scoped namespaces in code-behind. Preserve keyboard-friendly result entry, accessible responsive layouts, and automatic SignalR updates without manual refreshes.
