using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Core.Persistence;

namespace Mwi.SwissBowling.Core.Authentication;

public sealed class PostgresTicketStore(IDbContextFactory<WebStateDbContext> contexts, IDataProtectionProvider protection, TimeProvider clock) : ITicketStore
{
    private readonly IDataProtector protector = protection.CreateProtector("SwissBowling.AuthenticationTickets.v1");

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        ticket.Properties.Items["session-key"] = id;
        await using var database = await contexts.CreateDbContextAsync();
        database.AuthenticationSessions.Add(new AuthenticationSession
        {
            Id = id, Payload = Serialize(ticket), ExpiresAt = Expiry(ticket)
        });
        await database.SaveChangesAsync();
        return id;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await using var database = await contexts.CreateDbContextAsync();
        await database.AuthenticationSessions.Where(session => session.Id == key)
            .ExecuteUpdateAsync(update => update.SetProperty(session => session.Payload, Serialize(ticket)).SetProperty(session => session.ExpiresAt, Expiry(ticket)));
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var database = await contexts.CreateDbContextAsync();
        var session = await database.AuthenticationSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == key);
        if (session is null) return null;
        if (session.ExpiresAt <= clock.GetUtcNow())
        {
            await RemoveAsync(key);
            return null;
        }
        try { return Deserialize(session.Payload); }
        catch (CryptographicException) { await RemoveAsync(key); return null; }
    }

    public async Task RemoveAsync(string key)
    {
        await using var database = await contexts.CreateDbContextAsync();
        await database.AuthenticationSessions.Where(session => session.Id == key).ExecuteDeleteAsync();
    }

    public async Task<AuthenticationTicket?> RefreshAsync(string key, Func<AuthenticationTicket, Task<bool>> refresh, CancellationToken cancellationToken)
    {
        await using var database = await contexts.CreateDbContextAsync(cancellationToken);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var sessions = await database.AuthenticationSessions.FromSqlInterpolated($"SELECT * FROM \"AuthenticationSessions\" WHERE \"Id\" = {key} FOR UPDATE").ToListAsync(cancellationToken);
        var session = sessions.SingleOrDefault();
        if (session is null || session.ExpiresAt <= clock.GetUtcNow()) return null;
        AuthenticationTicket? ticket;
        try { ticket = Deserialize(session.Payload); }
        catch (CryptographicException) { return null; }
        if (ticket is null || !await refresh(ticket))
        {
            database.AuthenticationSessions.Remove(session);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        session.Payload = Serialize(ticket);
        session.ExpiresAt = Expiry(ticket);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ticket;
    }

    private byte[] Serialize(AuthenticationTicket ticket) => protector.Protect(TicketSerializer.Default.Serialize(ticket));
    private AuthenticationTicket? Deserialize(byte[] payload) => TicketSerializer.Default.Deserialize(protector.Unprotect(payload));
    private DateTimeOffset Expiry(AuthenticationTicket ticket) => ticket.Properties.ExpiresUtc ?? clock.GetUtcNow().AddHours(8);
}
