# Swiss Bowling – Meisterschaftsbetrieb & Resultaterfassung

Meisterschaften im Schweizer Kegeln (SFKV) verwalten, Resultate erfassen, Ranglisten öffentlich einsehen – inklusive Import
der aktuell auf [www.uvweb.kegler.ch](https://www.uvweb.kegler.ch) publizierten Ranglisten.

| Projekt | Inhalt |
|---|---|
| `SwissBowling.AppHost` | .NET Aspire: PostgreSQL (Container) + Server, Dashboard mit Telemetrie |
| `SwissBowling.Core` | Alles, was Server **und** Client nutzen: Verträge (DTOs/Requests mit DataAnnotations), API-Routen, Enums, Rangierung |
| `SwissBowling.Server` | REST API (Minimal APIs), EF Core + Identity + JWT, UVweb-Import, Blazor-Host (Prerendering, Interactive Server) |
| `SwissBowling.Client` | Blazor WebAssembly: Seiten, Layouts, API-Client, Anmeldung |
| `SwissBowling.Tests` | xUnit-Tests (Rangierung, PDF-Parser) |

Paketversionen werden zentral und **exakt** in `Directory.Packages.props` gepflegt (Central Package Management, keine Floating Versions).

## Fachliches Modell (SFKV)

* **Unterverband** (UV, Kürzel + UV-Nr.) mit **Vorstand**, **Kegelbahnen**, **Klubs** (Kurzname, Klubkategorie, Heimbahn, Klub-ID)
  und **Keglern** (Name «Nachname Vorname», Ort, **Lizenznummer** = SFKV-Mitglieder-ID, Geburtsdatum, Kategorie **A / B / C / G**).
  Kegler ohne Klub sind **Einzelmitglieder** ihres Unterverbands.
* Eine **Meisterschaft** ist ein Anlass (Durchführer, Lokal, Ort, Beginn/Ende, Anzahl Würfe). Status wird aus den Daten abgeleitet.
* **Disziplinen**: Meisterschaft und MS Damen. Pro Disziplin erzielt ein Kegler **ein Resultat**: Holz, **Neuner**, **Auszeichnung**, Matchdatum.
  Sport, Americaine, Sport Spezial, Kombination und Klubwertung werden nicht erfasst.
* **Rangliste pro Disziplin und Kategorie**: Holz absteigend, dann Neuner; bei Gleichstand geteilter Rang.
* Rollen: **Admin** (alles) und **Erfasser** (Resultate erfassen, solange die Meisterschaft läuft).

## Feature Slices

Jede fachliche Einheit liegt in **einem Ordner pro Projekt** mit demselben Namen:

```
Core/Results/ResultContracts.cs             Verträge + Routen
Server/Features/Results/Result.cs           Entität
Server/Features/Results/ResultEndpoints.cs  Endpunkte (lesen, Batch-Speichern, löschen)
Client/Features/Results/Admin/ResultEntry.* Resultaterfassung
```

Slices: `Associations` (inkl. Vorstand), `Venues` (Kegelbahnen), `Clubs`, `Players`, `Championships` (inkl. Rangliste), `Results`, `Import`, `Auth`, `Home` (nur Client).
Razor-Komponenten bestehen aus `.razor` (Markup), `.razor.cs` (Code-Behind) und bei Bedarf `.razor.css`.
Querschnittliches: `Client/Shared` (Layouts, `ApiPage`/`FormDialog`, Komponenten, Dienste), `Server/Infrastructure`, `Server/Data`.

## Import (`Server/Features/Import`)

**Stammdaten – Master: www.sfkv.ch/unterverbaende/uvXX.** `SfkvScraper` liest pro Unterverband Kopfdaten (Kürzel, UV-Nr., Homepage),
Vorstand, Mitglieder, Klubs und Kegelbahnen; `MasterDataImporter` übernimmt sie idempotent (UV-Nr., Klub-ID, Mitglieder-ID, Bahn-Name).
Nicht mehr gelistete Mitglieder werden deaktiviert. Die Klub-Zelle «Heimbahn» (`Bahnname, Klubort`) wird in `HomeVenue` und `City` aufgeteilt.

**Resultate – www.uvweb.kegler.ch.** `UvwebScraper` liest MS-Angaben, Navigation und die Ranglisten der Disziplinen 1 (Meisterschaft)
und 2 (MS Damen) pro Kategorie; `UvwebImporter` ordnet Kegler über die Lizenznummer und Klubs über die klubID (= SFKV Klub-ID) zu.
Unbekannte Kegler (z.B. Gäste) werden minimal angelegt.

**Archiv (seit 2005).** PDFs auf `ftp.sfkv.net`: `UvwebPdfParser` (PdfPig) erkennt die Disziplin-Abschnitte («MS», «MSD»; andere werden
übersprungen), Kategorie-Blöcke und Zeilen wie `1. Wicki Fridolin, River Boys 795 / 38`. Kegler werden über den Namen (bei Mehrdeutigkeit
Klub, dann Unterverband) zugeordnet, Klubs über den SFKV-Kurznamen – der stimmt mit den Stammdaten überein.

**Importlauf.** `ImportRunner` führt alles als einen Lauf im Hintergrund aus (Stammdaten aller UVs → aktuelle Ranglisten → optional noch
nicht importierte Archiv-PDFs aller UVs) und protokolliert jeden Schritt in `ImportRun`/`ImportLogEntry`; ein fehlerhafter Schritt bricht
den Lauf nicht ab. Admin: `/admin/import` – ein Knopf, Protokoll pro Lauf, Fortschritt per Polling. Es läuft höchstens ein Lauf gleichzeitig.

**Automatischer Abgleich:** `UvwebSyncService` startet periodisch einen Lauf ohne Archiv (`Import:SyncIntervalHours`, Standard 6, 0 = aus).

Die Nutzung der Daten ist mit dem Betreiber (Martin Schumacher) abgesprochen.

## Architektur-Entscheidungen

* **Ein Server-Prozess** für API und Blazor-Host: kein Proxy, kein CORS. Scalar-UI unter `/scalar` (Development).
* **Routen zentral in Core**: Server mappt und Client ruft dieselben Konstanten.
* **`IDbContextFactory<AppDbContext>`** statt direkt injiziertem Kontext: Endpoints und Importlauf erzeugen ihren eigenen, kurzlebigen `DbContext`; `EnrichNpgsqlDbContext` liefert Aspire-Retries, Health Check und Telemetrie.
* **Validierung einmal definiert**: DataAnnotations an den Request-Records, serverseitig über einen Endpoint-Filter geprüft.
* **Blazor Interactive Auto** mit Prerendering; JWT im Browser, Client-Dienste auch im Server registriert (`AddClientServices`).
* **SOLID**: Abstraktionen (`IApiClient`, `ITokenStore`, `ITokenService`), Basisklassen für wiederkehrende Abläufe, eine Aufgabe pro Klasse.

## Starten

Voraussetzungen: .NET SDK 10, Docker Desktop.

```powershell
dotnet run --project src/SwissBowling.AppHost
```

Development-Login: `admin@swissbowling.local` / `Admin123!`. Erster Schritt im Admin: `/admin/import` → «Import starten».
In Produktion `Jwt__SigningKey` (≥ 32 Zeichen) und `Seed__AdminPassword` als Secrets setzen.

Migrationen (ohne sie legt der Start das Schema per `EnsureCreated` an):

```powershell
dotnet ef migrations add Initial --project src/SwissBowling.Server --output-dir Data/Migrations
```

Tests: `dotnet test`
