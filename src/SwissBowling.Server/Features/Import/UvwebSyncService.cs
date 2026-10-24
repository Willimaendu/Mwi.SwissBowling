namespace SwissBowling.Server.Features.Import;

/// <summary>Startet periodisch einen Importlauf ohne Archiv (Konfiguration: Import:SyncIntervalHours, 0 = aus).</summary>
public sealed class UvwebSyncService(ImportRunner runner, IConfiguration configuration, ILogger<UvwebSyncService> logger) : BackgroundService
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
            try
            {
                var id = await runner.StartAsync(includeArchive: false, stoppingToken);
                logger.LogInformation(id is null ? "Sync übersprungen – es läuft bereits ein Import" : "Sync gestartet: Lauf {RunId}", id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Sync konnte nicht gestartet werden");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
