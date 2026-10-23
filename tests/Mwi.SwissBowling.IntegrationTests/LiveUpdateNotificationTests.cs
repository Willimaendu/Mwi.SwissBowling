using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mwi.SwissBowling.IntegrationTests;

[Collection("PostgreSQL")]
public sealed class LiveUpdateNotificationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task CommittedPrivateChangeNotifiesAdministrationOnly()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var messages = new List<string>();
        await using var listener = await ListenAsync(messages);
        await using var database = fixture.CreateContext();
        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Players\" SET \"FirstName\" = 'Changed' WHERE \"Id\" = {scenario.PlayerIds[0]}");
        Assert.True(await listener.WaitAsync(2000));
        while (await listener.WaitAsync(100)) { }
        Assert.Equal(["administration"], messages);
    }

    [Fact]
    public async Task RolledBackChangeDoesNotNotify()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var messages = new List<string>();
        await using var listener = await ListenAsync(messages);
        await using var database = fixture.CreateContext();
        await using var transaction = await database.Database.BeginTransactionAsync();
        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Players\" SET \"FirstName\" = 'Rolled back' WHERE \"Id\" = {scenario.PlayerIds[0]}");
        await transaction.RollbackAsync();
        Assert.False(await listener.WaitAsync(200));
        Assert.Empty(messages);
    }

    [Fact]
    public async Task SnapshotInsertNotifiesPublicWithoutSendingSnapshotData()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var messages = new List<string>();
        await using var listener = await ListenAsync(messages);
        await using var database = fixture.CreateContext();
        await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"PublishedSnapshots\" (\"EventId\", \"Revision\", \"Name\", \"Season\", \"PublishedAt\", \"Payload\") VALUES ({scenario.EventId}, 1, 'Test publication', 2026, now(), '{{}}'::jsonb)");
        Assert.True(await listener.WaitAsync(2000));
        while (await listener.WaitAsync(100)) { }
        Assert.Equal(["public"], messages);
    }

    [Fact]
    public async Task PublishedStateChangeNotifiesBothAudiences()
    {
        var scenario = await fixture.CreateScenarioAsync();
        var messages = new List<string>();
        await using var listener = await ListenAsync(messages);
        await using var database = fixture.CreateContext();
        await database.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Events\" SET \"State\" = 1, \"PublishedRevision\" = 1 WHERE \"Id\" = {scenario.EventId}");
        Assert.True(await listener.WaitAsync(2000));
        while (await listener.WaitAsync(100)) { }
        Assert.Equal(new[] { "administration", "public" }, messages.Order());
    }

    private async Task<NpgsqlConnection> ListenAsync(List<string> messages)
    {
        var connection = new NpgsqlConnection(fixture.ConnectionString);
        try
        {
            await connection.OpenAsync();
            connection.Notification += (_, notification) => messages.Add(notification.Payload);
            await using var command = new NpgsqlCommand("LISTEN swissbowling_changes", connection);
            await command.ExecuteNonQueryAsync();
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }
}
