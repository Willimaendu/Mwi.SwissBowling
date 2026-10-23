using Microsoft.AspNetCore.SignalR;
using Npgsql;

namespace Mwi.SwissBowling.Web.Features.LiveUpdates;

/// <summary>Fans out committed invalidations to clients connected to this replica.</summary>
public sealed class PostgresChangeListener(
    IConfiguration configuration,
    IHubContext<PublicUpdatesHub> publicHub,
    IHubContext<AdministrationUpdatesHub> administrationHub,
    ILogger<PostgresChangeListener> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("notifications")
            ?? configuration.GetConnectionString("application")
            ?? throw new InvalidOperationException("ConnectionStrings:notifications is required for live updates.");
        // LISTEN owns a dedicated session. Keepalive detects silent disconnections;
        // never return a subscribed connection to an application connection pool.
        var listenerConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = false,
            KeepAlive = 30
        }.ConnectionString;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(listenerConnectionString);
                await connection.OpenAsync(stoppingToken);
                var administrationChanged = false;
                var publicChanged = false;
                connection.Notification += (_, notification) =>
                {
                    administrationChanged |= notification.Payload == "administration";
                    publicChanged |= notification.Payload == "public";
                };
                await using var command = new NpgsqlCommand("LISTEN swissbowling_changes", connection);
                await command.ExecuteNonQueryAsync(stoppingToken);
                // NOTIFY is not a replay log: resynchronize after every listener reconnect.
                await PublishAsync(true, true, stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(stoppingToken);
                    if (!administrationChanged && !publicChanged) continue;
                    await PublishAsync(administrationChanged, publicChanged, stoppingToken);
                    administrationChanged = publicChanged = false;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) when (exception is NpgsqlException or IOException or TimeoutException)
            {
                // Never log connection strings, notification payloads or exception detail containing configuration.
                logger.LogWarning("Live-update listener disconnected ({ErrorType}); retrying.", exception.GetType().Name);
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }

    private async Task PublishAsync(bool administrationChanged, bool publicChanged, CancellationToken cancellationToken)
    {
        if (administrationChanged)
            await administrationHub.Clients.All.SendAsync("Changed", cancellationToken);
        if (publicChanged)
            await publicHub.Clients.All.SendAsync("Changed", cancellationToken);
    }
}
