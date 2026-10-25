using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Import;
using SwissBowling.Server.Data;

namespace SwissBowling.Server.Features.Import;

/// <summary>
/// Führt einen vollständigen Import-/Aktualisierungslauf im Hintergrund aus und protokolliert jeden Schritt:
/// 1. Stammdaten aller Unterverbände (sfkv.ch), 2. aktuelle Ranglisten (UVweb), 3. optional Archiv-PDFs aller
/// Unterverbände (nur noch nicht importierte Meisterschaften). Die Quellen werden parallel geladen, die Datenbank
/// sequenziell beschrieben. Es läuft höchstens ein Lauf gleichzeitig.
/// </summary>
public sealed class ImportRunner(IDbContextFactory<AppDbContext> dbFactory, IServiceScopeFactory scopes, ILogger<ImportRunner> logger)
{
    private const int Parallelism = 4;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Startet einen Lauf, falls keiner aktiv ist, und liefert dessen ID.</summary>
    public async Task<Guid?> StartAsync(bool includeArchive, CancellationToken ct)
    {
        if (!await _gate.WaitAsync(0, ct))
        {
            return null;
        }

        var run = new ImportRun { IncludeArchive = includeArchive };
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.ImportRuns.Add(run);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            _gate.Release();
            throw;
        }

        _ = Task.Run(() => ExecuteAsync(run.Id, includeArchive), CancellationToken.None);
        return run.Id;
    }

    private async Task ExecuteAsync(Guid runId, bool includeArchive)
    {
        try
        {
            // Ein Kontext für den ganzen Lauf; der Change Tracker wird nach jedem Schritt geleert.
            await using var db = await dbFactory.CreateDbContextAsync();
            var log = new RunLog(db, runId);
            var masterData = new MasterDataImporter(db);
            var importer = new UvwebImporter(db);

            // Die Scraper sind typisierte HttpClients und werden pro Lauf aufgelöst.
            using var scope = scopes.CreateScope();
            var sfkv = scope.ServiceProvider.GetRequiredService<SfkvScraper>();
            var uvweb = scope.ServiceProvider.GetRequiredService<UvwebScraper>();
            var ct = CancellationToken.None;

            await log.StepAsync("Stammdaten: Unterverbände von sfkv.ch laden", async () =>
            {
                var codes = await sfkv.GetAssociationCodesAsync(ct);
                await log.ImportAllAsync(codes,
                    code => $"Stammdaten {code}",
                    code => sfkv.GetAssociationAsync(code, ct),
                    source => masterData.ImportAsync(source, ct));
            });

            await log.StepAsync("Aktuelle Ranglisten von UVweb laden", async () =>
            {
                var current = await uvweb.GetCurrentAsync(ct);
                await log.ImportAllAsync(current,
                    item => $"{item.Association}: {item.Name}",
                    async item => (Championship: await uvweb.GetChampionshipAsync(item.MsId, item.UvId, ct), Results: await uvweb.GetResultsAsync(item.MsId, item.UvId, item.RanglisteId, ct)),
                    data => importer.ImportAsync(data.Championship, data.Results, ct));
            });

            if (includeArchive)
            {
                var imported = (await db.Championships.Where(c => c.ExternalId != null).Select(c => c.ExternalId!.Value).ToListAsync(ct)).ToHashSet();
                var associations = await db.Associations.Where(a => a.ExternalId != null).OrderBy(a => a.Name).ToListAsync(ct);

                foreach (var association in associations)
                {
                    await log.StepAsync($"Archiv {association.Name} laden", async () =>
                    {
                        var archive = await uvweb.GetArchiveAsync(association.ExternalId!.Value, ct);
                        var pending = archive.Where(e => imported.Add(e.MsId)).ToList();
                        await log.ImportAllAsync(pending,
                            entry => $"{association.Name}: {entry.Name} (Archiv)",
                            async entry =>
                            {
                                var championship = await uvweb.GetChampionshipAsync(entry.MsId, entry.UvId, ct);
                                await using var pdf = await uvweb.GetPdfAsync(entry.PdfUrl, ct);
                                return (Championship: championship, Results: UvwebPdfParser.Parse(pdf));
                            },
                            data => importer.ImportArchiveAsync(data.Championship, data.Results, ct));
                    });
                }
            }

            await log.FinishAsync(ImportRunStatus.Completed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Importlauf {RunId} abgebrochen", runId);
            await MarkFailedAsync(runId, ex.Message);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task MarkFailedAsync(Guid runId, string message)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            db.ImportLogEntries.Add(new ImportLogEntry { RunId = runId, Source = "Lauf abgebrochen", Message = message, IsError = true });
            await db.SaveChangesAsync();
            await new RunLog(db, runId).FinishAsync(ImportRunStatus.Failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Importlauf {RunId} konnte nicht als fehlgeschlagen markiert werden", runId);
        }
    }

    /// <summary>
    /// Schreibt Fortschritt und Protokolleinträge sofort in die Datenbank, damit die Oberfläche mitlesen kann.
    /// Nach jedem Schritt wird der Change Tracker geleert, damit lange Läufe nicht immer mehr Speicher binden.
    /// </summary>
    private sealed class RunLog(AppDbContext db, Guid runId)
    {
        public async Task StepAsync(string step, Func<Task> action)
        {
            await db.ImportRuns.Where(r => r.Id == runId).ExecuteUpdateAsync(s => s.SetProperty(r => r.CurrentStep, step));
            await RunAsync(step, async () =>
            {
                await action();
                return null;
            });
        }

        /// <summary>
        /// Lädt die Quellen parallel (höchstens <see cref="Parallelism"/> gleichzeitig, um die Webseiten nicht zu überlasten)
        /// und übernimmt sie in der ursprünglichen Reihenfolge nacheinander in die Datenbank – der DbContext ist nicht threadsicher.
        /// </summary>
        public async Task ImportAllAsync<TItem, TData>(IEnumerable<TItem> items, Func<TItem, string> source, Func<TItem, Task<TData>> fetch, Func<TData, Task<ImportSummary>> import)
        {
            using var gate = new SemaphoreSlim(Parallelism);
            var fetching = items.Select(item => (Source: source(item), Data: FetchAsync(gate, () => fetch(item)))).ToList();

            foreach (var (label, data) in fetching)
            {
                await RunAsync(label, async () => await import(await data));
            }
        }

        private static async Task<T> FetchAsync<T>(SemaphoreSlim gate, Func<Task<T>> fetch)
        {
            await gate.WaitAsync();
            try
            {
                return await fetch();
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>Führt einen Schritt aus und protokolliert Ergebnis oder Fehler.</summary>
        private async Task RunAsync(string source, Func<Task<ImportSummary?>> action)
        {
            try
            {
                if (await action() is { } summary)
                {
                    db.ImportLogEntries.Add(new ImportLogEntry { RunId = runId, Source = summary.Source, Clubs = summary.Clubs, Players = summary.Players, Results = summary.Results });
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ein einzelner fehlerhafter Schritt bricht den Lauf nicht ab.
                db.ChangeTracker.Clear();
                db.ImportLogEntries.Add(new ImportLogEntry { RunId = runId, Source = source, Message = ex.Message, IsError = true });
                await db.SaveChangesAsync();
            }
            finally
            {
                db.ChangeTracker.Clear();
            }
        }

        public Task FinishAsync(ImportRunStatus status) =>
            db.ImportRuns.Where(r => r.Id == runId).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.FinishedAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.CurrentStep, (string?)null));
    }
}
