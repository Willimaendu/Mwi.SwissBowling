using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Api.Features.Setup;
using Mwi.SwissBowling.Api.Features.Standings;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Publication;

public sealed class PublicationService(ApplicationDbContext database, TimeProvider clock)
{
    public async Task<PublicEventDto> PublishAsync(Guid eventId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var championship = await EventAccess.LockAsync(database, eventId, cancellationToken);
        var results = await ResultQueries.ReadAsync(database, eventId, cancellationToken);
        var now = clock.GetUtcNow();
        var activeLeases = await database.Set<EditingLease>().AnyAsync(row => row.EventId == eventId && row.ExpiresAt > now, cancellationToken);
        PublicationRules.RequirePublishable(championship.State, results.Length, activeLeases);
        foreach (var result in results)
        {
            var error = ScoreRules.Validate(new ResultInput(result.Total, result.Nines, result.Eights, result.Interrupted, result.Version, Guid.Empty), championship.MaximumThrows);
            if (error is not null) throw new RequestRejectedException(409, "Ein Resultat ist nicht gültig: " + error);
        }
        var annual = await database.AnnualChampionships.SingleAsync(row => row.Id == championship.AnnualChampionshipId, cancellationToken);
        var year = await database.Seasons.Where(row => row.Id == annual.SeasonId).Select(row => row.Year).SingleAsync(cancellationToken);
        var venue = await database.Venues.Where(row => row.Id == championship.VenueId).Select(row => row.Name).SingleAsync(cancellationToken);
        var snapshot = new PublicEventDto(eventId, championship.Name, venue, year, championship.StartDate, championship.EndDate, now,
            championship.PublishedRevision + 1, false, StandingRules.Calculate(results));
        database.Add(new PublishedSnapshot { EventId = eventId, Revision = snapshot.Revision, Name = snapshot.Name, Season = year, PublishedAt = now, Payload = JsonSerializer.Serialize(snapshot) });
        championship.PublishedRevision = snapshot.Revision;
        championship.State = EventState.Published;
        database.Audit("EventPublished", eventId.ToString(), user.Issuer(), user.Subject(), now, JsonSerializer.Serialize(new { snapshot.Revision }));
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return snapshot;
    }

    public async Task<Guid> RequestCorrectionAsync(Guid eventId, string reason, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1000) throw new RequestRejectedException(400, "Bitte einen Korrekturgrund mit höchstens 1000 Zeichen angeben.");
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var championship = await EventAccess.LockAsync(database, eventId, cancellationToken);
        if (championship.State != EventState.Published) throw new RequestRejectedException(409, "Nur veröffentlichte Meisterschaften können wiedereröffnet werden.");
        if (await database.Set<CorrectionRequest>().AnyAsync(row => row.EventId == eventId && row.ApprovedAt == null, cancellationToken)) throw new RequestRejectedException(409, "Eine Anfrage wartet bereits auf Genehmigung.");
        var request = new CorrectionRequest { Id = Guid.NewGuid(), EventId = eventId, Reason = reason.Trim(), RequestedIssuer = user.Issuer(), RequestedSubject = user.Subject(), RequestedAt = clock.GetUtcNow() };
        database.Add(request);
        database.Audit("CorrectionRequested", eventId.ToString(), user.Issuer(), user.Subject(), clock.GetUtcNow(), JsonSerializer.Serialize(new { request.Id }));
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return request.Id;
    }

    public async Task ApproveCorrectionAsync(Guid eventId, Guid requestId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var championship = await EventAccess.LockAsync(database, eventId, cancellationToken);
        var request = await database.Set<CorrectionRequest>().SingleOrDefaultAsync(row => row.Id == requestId && row.EventId == eventId, cancellationToken)
            ?? throw new RequestRejectedException(404, "Die Korrekturanfrage ist nicht vorhanden.");
        if (championship.State != EventState.Published || request.ApprovedAt is not null) throw new RequestRejectedException(409, "Diese Anfrage kann nicht mehr genehmigt werden.");
        PublicationRules.RequireDifferentApprover(request.RequestedIssuer, request.RequestedSubject, user.Issuer(), user.Subject());
        request.ApprovedAt = clock.GetUtcNow(); request.ApprovedIssuer = user.Issuer(); request.ApprovedSubject = user.Subject();
        championship.State = EventState.Correcting;
        database.Audit("CorrectionApproved", eventId.ToString(), user.Issuer(), user.Subject(), clock.GetUtcNow(), JsonSerializer.Serialize(new { request.Id }));
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PublicEventDto?> ReadPublicAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var data = await (from snapshot in database.Set<PublishedSnapshot>().AsNoTracking()
                          join championship in database.Events on new { snapshot.EventId, snapshot.Revision } equals new { EventId = championship.Id, Revision = championship.PublishedRevision }
                          where snapshot.EventId == eventId
                          select new { snapshot.Payload, championship.State }).SingleOrDefaultAsync(cancellationToken);
        if (data is null) return null;
        var result = JsonSerializer.Deserialize<PublicEventDto>(data.Payload) ?? throw new InvalidOperationException("Published snapshot is invalid.");
        return result with { UnderCorrection = data.State == EventState.Correcting };
    }

    public Task<PublicEventSummary[]> ListPublicAsync(CancellationToken cancellationToken) =>
        (from snapshot in database.Set<PublishedSnapshot>().AsNoTracking()
         join championship in database.Events on new { snapshot.EventId, snapshot.Revision } equals new { EventId = championship.Id, Revision = championship.PublishedRevision }
         orderby snapshot.PublishedAt descending, snapshot.EventId
         select new PublicEventSummary(snapshot.EventId, snapshot.Name, snapshot.Season, snapshot.PublishedAt, snapshot.Revision, championship.State == EventState.Correcting)).ToArrayAsync(cancellationToken);

    public Task<CorrectionDto[]> PendingAsync(CancellationToken cancellationToken) =>
        (from request in database.Set<CorrectionRequest>().AsNoTracking()
         join championship in database.Events on request.EventId equals championship.Id
         where request.ApprovedAt == null
         orderby request.RequestedAt
         select new CorrectionDto(request.Id, request.EventId, championship.Name, request.Reason, request.RequestedSubject, request.RequestedAt)).ToArrayAsync(cancellationToken);
}
