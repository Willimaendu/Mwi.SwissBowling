using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Publication;

public sealed class PublicationQueries(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<PublicEventDto?> ReadPublicAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        var data = await (from snapshot in database.Set<PublishedSnapshot>().AsNoTracking()
                          join championship in database.Events on new { snapshot.EventId, snapshot.Revision } equals new { EventId = championship.Id, Revision = championship.PublishedRevision }
                          where snapshot.EventId == eventId
                          select new { snapshot.Payload, championship.State }).SingleOrDefaultAsync(cancellationToken);
        if (data is null) return null;
        var result = JsonSerializer.Deserialize<PublicEventDto>(data.Payload) ?? throw new InvalidOperationException("Published snapshot is invalid.");
        return result with { UnderCorrection = data.State == EventState.Correcting };
    }

    public async Task<PublicEventSummary[]> ListPublicAsync(CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        return await (from snapshot in database.Set<PublishedSnapshot>().AsNoTracking()
         join championship in database.Events on new { snapshot.EventId, snapshot.Revision } equals new { EventId = championship.Id, Revision = championship.PublishedRevision }
         orderby snapshot.PublishedAt descending, snapshot.EventId
         select new PublicEventSummary(snapshot.EventId, snapshot.Name, snapshot.Season, snapshot.PublishedAt, snapshot.Revision, championship.State == EventState.Correcting)).ToArrayAsync(cancellationToken);
    }

    public async Task<CorrectionDto[]> PendingAsync(CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        return await (from request in database.Set<CorrectionRequest>().AsNoTracking()
         join championship in database.Events on request.EventId equals championship.Id
         where request.ApprovedAt == null
         orderby request.RequestedAt
         select new CorrectionDto(request.Id, request.EventId, championship.Name, request.Reason, request.RequestedSubject, request.RequestedAt)).ToArrayAsync(cancellationToken);
    }
}
