using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Import;
using SwissBowling.Server.Data;

namespace SwissBowling.Server.Features.Import;

/// <summary>
/// Führt einen vollständigen Import-/Aktualisierungslauf im Hintergrund aus und protokolliert jeden Schritt:
/// 1. Stammdaten aller Unterverbände (sfkv.ch), 2. aktuelle Ranglisten (UVweb), 3. optional Archiv-PDFs aller
/// Unterverbände (nur noch nicht importierte Meisterschaften). Es läuft höchstens ein Lauf gleichzeitig.
/// </summary>
public sealed class ImportRunner(IServiceScopeFactory scopes, ILogger<ImportRunner> logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsRunning => _gate.CurrentCount == 0;

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
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var log = new RunLog(db, runId);

            var masterData = scope.ServiceProvider.GetRequiredService<MasterDataImporter>();
            var scraper = scope.ServiceProvider.GetRequiredService<UvwebScraper>();
            var importer = scope.ServiceProvider.GetRequiredService<UvwebImporter>();

            await log.StepAsync("Stammdaten: Unterverbände von sfkv.ch laden", async () =>
            {
                foreach (var code in await masterData.GetAssociationCodesAsync(CancellationToken.None))
                {
                    await log.TryAsync($"Stammdaten {code}", () => masterData.ImportAssociationAsync(code, CancellationToken.None));
                }
            });

            await log.StepAsync("Aktuelle Ranglisten von UVweb laden", async () =>
            {
                foreach (var item in await scraper.GetCurrentAsync(CancellationToken.None))
                {
                    await log.TryAsync($"{item.Association}: {item.Name}", () => importer.ImportAsync(item.MsId, item.UvId, item.RanglisteId, CancellationToken.None));
                }
            });

            if (includeArchive)
            {
                var imported = (await db.Championships.Where(c => c.ExternalId != null).Select(c => c.ExternalId!.Value).ToListAsync()).ToHashSet();
                var associations = await db.Associations.Where(a => a.ExternalId != null).OrderBy(a => a.Name).ToListAsync();

                foreach (var association in associations)
                {
                    await log.StepAsync($"Archiv {association.Name} laden", async () =>
                    {
                        var archive = await scraper.GetArchiveAsync(association.ExternalId!.Value, CancellationToken.None);
                        foreach (var entry in archive.Where(e => !imported.Contains(e.MsId)))
                        {
                            await log.TryAsync($"{association.Name}: {entry.Name} (Archiv)", () => importer.ImportArchiveAsync(entry.MsId, entry.UvId, entry.PdfUrl, CancellationToken.None));
                            imported.Add(entry.MsId);
                        }
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
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
            await TryAsync(step, async () =>
            {
                await action();
                return null;
            });
        }

        public async Task TryAsync(string source, Func<Task<ImportSummary?>> action)
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

        public Task TryAsync(string source, Func<Task<ImportSummary>> action) =>
            TryAsync(source, async () => (ImportSummary?)await action());

        public Task FinishAsync(ImportRunStatus status) =>
            db.ImportRuns.Where(r => r.Id == runId).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.FinishedAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.CurrentStep, (string?)null));
    }
}
