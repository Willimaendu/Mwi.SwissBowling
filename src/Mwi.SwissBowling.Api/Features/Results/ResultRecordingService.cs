using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Results;

public sealed class ResultRecordingService(ApplicationDbContext database, TimeProvider clock, IOptions<EditingLeaseOptions> options)
{
    public async Task<LeaseDto> AcquireAsync(Guid eventId, Guid playerId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var championship = await EventAccess.LockAsync(database, eventId, cancellationToken);
        EventAccess.RequireEditable(championship);
        var seasonId = await EventAccess.SeasonIdAsync(database, championship, cancellationToken);
        if (!await database.SeasonMembers.AnyAsync(row => row.SeasonId == seasonId && row.PlayerId == playerId, cancellationToken)) throw new RequestRejectedException(400, "Der Spieler ist dieser Saison nicht zugeordnet.");
        var lease = await database.Set<EditingLease>().FindAsync([eventId, playerId], cancellationToken);
        var now = clock.GetUtcNow();
        if (lease is not null && lease.ExpiresAt > now) throw new RequestRejectedException(409, "Das Resultat wird bereits bearbeitet. Warten Sie auf Freigabe oder Ablauf der Sperre.");
        if (lease is null) { lease = new EditingLease { EventId = eventId, PlayerId = playerId }; database.Add(lease); }
        lease.Issuer = user.Issuer(); lease.Subject = user.Subject(); lease.Token = Guid.NewGuid();
        lease.ExpiresAt = now.AddSeconds(options.Value.DurationSeconds);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(lease);
    }

    public async Task<LeaseDto> RenewAsync(Guid eventId, Guid playerId, Guid token, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        EventAccess.RequireEditable(await EventAccess.LockAsync(database, eventId, cancellationToken));
        var lease = await RequireOwnedLeaseAsync(eventId, playerId, token, user, cancellationToken);
        lease.ExpiresAt = clock.GetUtcNow().AddSeconds(options.Value.DurationSeconds);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(lease);
    }

    public async Task ReleaseAsync(Guid eventId, Guid playerId, Guid token, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await EventAccess.LockAsync(database, eventId, cancellationToken);
        var lease = await database.Set<EditingLease>().FindAsync([eventId, playerId], cancellationToken);
        if (lease is not null)
        {
            if (!Owned(lease, token, user)) throw new RequestRejectedException(409, "Diese Bearbeitungssperre gehört nicht zu Ihrer Sitzung.");
            database.Remove(lease);
            await database.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<long> SaveAsync(Guid eventId, Guid playerId, ResultInput input, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var championship = await EventAccess.LockAsync(database, eventId, cancellationToken);
        EventAccess.RequireEditable(championship);
        var validation = ScoreRules.Validate(input, championship.MaximumThrows);
        if (validation is not null) throw new RequestRejectedException(400, validation);
        await RequireOwnedLeaseAsync(eventId, playerId, input.LeaseToken, user, cancellationToken);
        var seasonId = await EventAccess.SeasonIdAsync(database, championship, cancellationToken);
        var seasons = await database.Seasons.FromSqlInterpolated($"SELECT * FROM \"Seasons\" WHERE \"Id\" = {seasonId} FOR UPDATE").ToListAsync(cancellationToken);
        var season = seasons.Single();
        if (!await database.SeasonMembers.AnyAsync(row => row.SeasonId == seasonId && row.PlayerId == playerId, cancellationToken)) throw new RequestRejectedException(400, "Die Saisonzuordnung fehlt.");
        var result = await database.Set<PlayerResult>().FindAsync([eventId, playerId], cancellationToken);
        if ((result?.Version ?? 0) != input.ExpectedVersion) throw new RequestRejectedException(409, "Das Resultat wurde verändert. Laden Sie den aktuellen Stand neu.");
        var previous = result is null ? null : new { result.Total, result.Nines, result.Eights, result.Interrupted, result.Version };
        if (result is null) { result = new PlayerResult { EventId = eventId, PlayerId = playerId, SeasonId = seasonId }; database.Add(result); }
        result.Total = input.Total; result.Nines = input.Nines; result.Eights = input.Eights; result.Interrupted = input.Interrupted;
        result.Version++;
        season.RosterLocked = true;
        database.Audit("ResultSaved", $"{eventId}/{playerId}", user.Issuer(), user.Subject(), clock.GetUtcNow(),
            JsonSerializer.Serialize(new { Previous = previous, Current = new { result.Total, result.Nines, result.Eights, result.Interrupted, result.Version } }));
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result.Version;
    }

    private async Task<EditingLease> RequireOwnedLeaseAsync(Guid eventId, Guid playerId, Guid token, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var lease = await database.Set<EditingLease>().FindAsync([eventId, playerId], cancellationToken);
        if (lease is null || !Owned(lease, token, user) || lease.ExpiresAt <= clock.GetUtcNow()) throw new RequestRejectedException(409, "Die Bearbeitungssperre ist abgelaufen oder gehört nicht zu Ihrer Sitzung.");
        return lease;
    }
    private static bool Owned(EditingLease lease, Guid token, ClaimsPrincipal user) => lease.Token == token && lease.Subject == user.Subject() && lease.Issuer == user.Issuer();
    private LeaseDto ToDto(EditingLease lease) => new(lease.Token, lease.ExpiresAt, options.Value.RenewalSeconds);
}
