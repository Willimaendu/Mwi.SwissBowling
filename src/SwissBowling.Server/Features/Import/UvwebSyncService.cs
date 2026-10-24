namespace SwissBowling.Server.Features.Import;

/// <summary>
/// Gleicht periodisch ab: zuerst die Stammdaten von sfkv.ch, dann die aktuell auf UVweb publizierten Ranglisten
/// (Konfiguration: Import:SyncIntervalHours, 0 = aus).
/// </summary>
public sealed class UvwebSyncService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<UvwebSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = configuration.GetValue("Import:SyncIntervalHours", 0);
        if (hours <= 0)
        {
            return;
        }

        // Erst nach der Datenbank-Initialisierung loslegen.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));
        do
        {
            await SyncAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();

        try
        {
            var masterData = scope.ServiceProvider.GetRequiredService<MasterDataImporter>();
            foreach (var summary in await masterData.ImportAllAsync(ct))
            {
                logger.LogInformation("Stammdaten-Sync: {Source} – {Clubs} Klubs, {Players} Mitglieder", summary.Source, summary.Clubs, summary.Players);
            }

            var scraper = scope.ServiceProvider.GetRequiredService<UvwebScraper>();
            var importer = scope.ServiceProvider.GetRequiredService<UvwebImporter>();
            foreach (var item in await scraper.GetCurrentAsync(ct))
            {
                var summary = await importer.ImportAsync(item.MsId, item.UvId, item.RanglisteId, ct);
                logger.LogInformation("UVweb-Sync: {Source} – {Results} Resultate", summary.Source, summary.Results);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Ein fehlgeschlagener Lauf darf den Dienst nicht beenden – nächster Versuch beim nächsten Tick.
            logger.LogWarning(ex, "Sync fehlgeschlagen");
        }
    }
}
