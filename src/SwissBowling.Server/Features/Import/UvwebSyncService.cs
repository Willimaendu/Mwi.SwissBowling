namespace SwissBowling.Server.Features.Import;

/// <summary>Gleicht die aktuell auf UVweb publizierten Ranglisten periodisch ab (Konfiguration: Import:SyncIntervalHours, 0 = aus).</summary>
public sealed class UvwebSyncService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<UvwebSyncService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = configuration.GetValue("Import:SyncIntervalHours", 0);
        if (hours <= 0)
        {
            return;
        }

        // Erst nach dem Start der Datenbank-Initialisierung loslegen.
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
        var scraper = scope.ServiceProvider.GetRequiredService<UvwebScraper>();
        var importer = scope.ServiceProvider.GetRequiredService<UvwebImporter>();

        try
        {
            var current = await scraper.GetCurrentAsync(ct);
            foreach (var item in current)
            {
                var summary = await importer.ImportAsync(item.MsId, item.UvId, item.RanglisteId, ct);
                logger.LogInformation("UVweb-Sync: {Championship} – {Results} Resultate", summary.Championship, summary.Results);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Ein fehlgeschlagener Lauf darf den Dienst nicht beenden – nächster Versuch beim nächsten Tick.
            logger.LogWarning(ex, "UVweb-Sync fehlgeschlagen");
        }
    }
}
