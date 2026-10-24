# Swiss Bowling – Meisterschaftsbetrieb & Resultaterfassung

Einzelmeisterschaften im Schweizer Kegeln planen, Resultate pro Runde erfassen, Ranglisten öffentlich einsehen.

| Projekt | Inhalt |
|---|---|
| `SwissBowling.AppHost` | .NET Aspire: PostgreSQL (Container) + Server, Dashboard mit Telemetrie |
| `SwissBowling.Core` | Alles, was Server **und** Client nutzen: Verträge (DTOs/Requests mit DataAnnotations), API-Routen, Enums, Ranglisten-Logik |
| `SwissBowling.Server` | REST API (Minimal APIs), EF Core + Identity + JWT, Blazor-Host (Prerendering, Interactive Server) |
| `SwissBowling.Client` | Blazor WebAssembly: Seiten, Layouts, API-Client, Anmeldung |
| `SwissBowling.Core.Tests` | xUnit-Tests der Ranglisten-Logik |

## Feature Slices

Jede fachliche Einheit liegt in **einem Ordner pro Projekt** mit demselben Namen. Wer z.B. etwas an den Resultaten ändert, findet alles hier:

```
Core/Results/ResultContracts.cs            Verträge + Routen (ResultRoutes)
Server/Features/Results/Result.cs          Entität
Server/Features/Results/ResultEndpoints.cs Endpunkte (lesen, Batch-Speichern, löschen)
Client/Features/Results/RoundDetail.*      öffentliche Rundenansicht
Client/Features/Results/Admin/ResultEntry.* Resultaterfassung
```

Slices: `Auth` (Login, Benutzer), `Clubs`, `Players`, `Seasons`, `Championships` (inkl. Runden, Rangliste), `Results`, `Home` (nur Client).

Jede Razor-Komponente besteht aus `.razor` (Markup), `.razor.cs` (Code-Behind) und bei Bedarf `.razor.css` (scoped Styles).
Querschnittliches liegt in `Client/Shared` (Layouts, `ApiPage`/`FormDialog`-Basisklassen, Komponenten, Dienste) und
`Server/Infrastructure` (Validierung, ProblemDetails, Telemetrie) bzw. `Server/Data` (DbContext, Initialisierung).

## Architektur-Entscheidungen

* **Ein Server-Prozess** für API und Blazor-Host: keine Service-to-Service-Kommunikation, kein Proxy, kein CORS.
  Die API bleibt eine reine REST-Schnittstelle (`/api/...`, Scalar-UI unter `/scalar` in Development).
* **Routen zentral in Core** (`ClubRoutes`, `PlayerRoutes`, …): Server mappt und Client ruft dieselben Konstanten – eine Quelle der Wahrheit.
* **Validierung einmal definiert**: DataAnnotations an den Request-Records in Core, serverseitig über einen Endpoint-Filter geprüft.
* **Status wird abgeleitet statt gepflegt**: Meisterschaftsstatus ergibt sich aus abgeschlossenen Runden.
* **Blazor Interactive Auto**: Prerendering für den Erstaufruf, danach Server-Interaktivität bis die WebAssembly-Runtime gecacht ist.
  Das JWT liegt im Browser; der `ITokenStore` liest es per JS-Interop, daher sind die Client-Dienste auch im Server registriert.
* **SOLID**: Endpunkte und Seiten hängen von Abstraktionen (`IApiClient`, `ITokenStore`, `ITokenService`); Basisklassen
  (`ApiPage`, `FormDialog`) kapseln wiederkehrende Abläufe; jede Klasse hat genau eine Aufgabe.

## Starten

Voraussetzungen: .NET SDK 10 (siehe `global.json`), Docker Desktop.

```powershell
dotnet run --project src/SwissBowling.AppHost
```

Development-Login: `admin@swissbowling.local` / `Admin123!` (aus `appsettings.Development.json`).
In Produktion `Jwt__SigningKey` (≥ 32 Zeichen) und `Seed__AdminPassword` als Secrets setzen.

### Migrationen

Ohne Migrationen wird das Schema beim Start per `EnsureCreated` angelegt. Für produktive Nutzung:

```powershell
dotnet ef migrations add Initial --project src/SwissBowling.Server --output-dir Data/Migrations
```

### Tests

```powershell
dotnet test
```

## Fachliches Modell

Eine **Meisterschaft** (Saison, *n* Runden, *k* Streichresultate, Würfe pro Runde) hat **Runden** (Datum, Kegelbahn).
Pro Runde erfasst ein Kegler ein **Resultat** in Holz (max. 9 × Würfe). Die **Rangliste** summiert die besten *n − k* Runden;
Gleichstand entscheidet das beste Einzelresultat, sonst wird der Rang geteilt. Ranglisten gibt es gesamt und pro **Kategorie**.
Abgeschlossene Runden dürfen nur Administratoren ändern; **Erfasser** erfassen Resultate und schliessen Runden ab.
