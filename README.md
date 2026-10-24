# Swiss Bowling – Meisterschaftsbetrieb & Resultaterfassung

Meisterschaften im Schweizer Kegeln (SFKV) verwalten, Resultate erfassen, Ranglisten öffentlich einsehen – inklusive Import
der aktuell auf [www.uvweb.kegler.ch](https://www.uvweb.kegler.ch) publizierten Ranglisten.

| Projekt | Inhalt |
|---|---|
| `SwissBowling.AppHost` | .NET Aspire: PostgreSQL (Container) + Server, Dashboard mit Telemetrie |
| `SwissBowling.Core` | Alles, was Server **und** Client nutzen: Verträge (DTOs/Requests mit DataAnnotations), API-Routen, Enums, Rangierung |
| `SwissBowling.Server` | REST API (Minimal APIs), EF Core + Identity + JWT, UVweb-Import, Blazor-Host (Prerendering, Interactive Server) |
| `SwissBowling.Client` | Blazor WebAssembly: Seiten, Layouts, API-Client, Anmeldung |
| `SwissBowling.Core.Tests` | xUnit-Tests der Rangierung |

## Fachliches Modell (SFKV)

* **Unterverband** (UV) → **Klub** → **Kegler** (Name «Nachname Vorname», Ort, **Lizenznummer**, Kategorie **A / B / C / G**).
* Eine **Meisterschaft** ist ein Anlass (Durchführer, Lokal, Ort, Beginn/Ende, Anzahl Würfe). Status wird aus den Daten abgeleitet.
* Jeder Kegler erzielt pro Meisterschaft **ein Resultat**: Holz, **Neuner**, **Auszeichnung**, Matchdatum.
* **Rangliste pro Kategorie**: Holz absteigend, dann Neuner; bei Gleichstand geteilter Rang.
* Rollen: **Admin** (alles) und **Erfasser** (Resultate erfassen, solange die Meisterschaft läuft).

## Feature Slices

Jede fachliche Einheit liegt in **einem Ordner pro Projekt** mit demselben Namen:

```
Core/Results/ResultContracts.cs             Verträge + Routen
Server/Features/Results/Result.cs           Entität
Server/Features/Results/ResultEndpoints.cs  Endpunkte (lesen, Batch-Speichern, löschen)
Client/Features/Results/Admin/ResultEntry.* Resultaterfassung
```

Slices: `Associations`, `Clubs`, `Players`, `Championships` (inkl. Rangliste), `Results`, `Import` (UVweb), `Auth`, `Home` (nur Client).
Razor-Komponenten bestehen aus `.razor` (Markup), `.razor.cs` (Code-Behind) und bei Bedarf `.razor.css`.
Querschnittliches: `Client/Shared` (Layouts, `ApiPage`/`FormDialog`, Komponenten, Dienste), `Server/Infrastructure`, `Server/Data`.

## Import von UVweb

`Server/Features/Import`: `UvwebScraper` liest die öffentlichen HTML-Seiten (Übersicht «Aktuelle Ranglisten», MS-Angaben, Navigation mit
Kategorien, Rangliste pro Kategorie, Klubresultate inkl. Mitglieder), `UvwebImporter` übernimmt sie idempotent:
Unterverband über `uvID`, Klub über `klubID`, Kegler über Lizenznummer, Meisterschaft über `msID`. Admin-Seite: `/admin/import`.

**Archiv (seit 2005):** Die archivierten Ranglisten liegen als textbasierte PDFs auf `ftp.sfkv.net`. `UvwebPdfParser` (PdfPig) liest
Kategorie-Blöcke und Zeilen wie `1. Wicki Fridolin, River Boys 795 / 38`, die Stammdaten kommen weiterhin aus `msangaben.aspx`.
Da PDFs keine Lizenznummern enthalten, werden Kegler über den Namen (bei Mehrdeutigkeit über den Klub) zugeordnet und Klubs über
den Kurznamen («Lüderenblick» ↔ «KK Lüderenblick»). Ein späterer HTML-Import ergänzt bei solchen Keglern die Lizenznummer.

**Automatischer Abgleich:** `UvwebSyncService` importiert die aktuellen Ranglisten periodisch (`Import:SyncIntervalHours`, Standard 6, 0 = aus).

Die Nutzung der Daten ist mit dem Betreiber (Martin Schumacher) abgesprochen.

## Architektur-Entscheidungen

* **Ein Server-Prozess** für API und Blazor-Host: kein Proxy, kein CORS. Scalar-UI unter `/scalar` (Development).
* **Routen zentral in Core**: Server mappt und Client ruft dieselben Konstanten.
* **Validierung einmal definiert**: DataAnnotations an den Request-Records, serverseitig über einen Endpoint-Filter geprüft.
* **Blazor Interactive Auto** mit Prerendering; JWT im Browser, Client-Dienste auch im Server registriert (`AddClientServices`).
* **SOLID**: Abstraktionen (`IApiClient`, `ITokenStore`, `ITokenService`), Basisklassen für wiederkehrende Abläufe, eine Aufgabe pro Klasse.

## Starten

Voraussetzungen: .NET SDK 10, Docker Desktop.

```powershell
dotnet run --project src/SwissBowling.AppHost
```

Development-Login: `admin@swissbowling.local` / `Admin123!`. Erster Schritt im Admin: `/admin/import` → «Alle importieren».
In Produktion `Jwt__SigningKey` (≥ 32 Zeichen) und `Seed__AdminPassword` als Secrets setzen.

Migrationen (ohne sie legt der Start das Schema per `EnsureCreated` an):

```powershell
dotnet ef migrations add Initial --project src/SwissBowling.Server --output-dir Data/Migrations
```

Tests: `dotnet test`
