using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Setup;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Results;

public static class EventAccess
{
    public static async Task<ChampionshipEvent> LockAsync(ApplicationDbContext database, Guid eventId, CancellationToken cancellationToken)
    {
        var events = await database.Events.FromSqlInterpolated($"SELECT * FROM \"Events\" WHERE \"Id\" = {eventId} FOR UPDATE").ToListAsync(cancellationToken);
        return events.SingleOrDefault() ?? throw new RequestRejectedException(404, "Die Meisterschaft ist nicht vorhanden.");
    }
    public static void RequireEditable(ChampionshipEvent championship)
    {
        if (championship.State == EventState.Published) throw new RequestRejectedException(409, "Veröffentlichte Resultate benötigen zuerst eine genehmigte Wiedereröffnung.");
    }
    public static Task<Guid> SeasonIdAsync(ApplicationDbContext database, ChampionshipEvent championship, CancellationToken cancellationToken) => database.AnnualChampionships.Where(row => row.Id == championship.AnnualChampionshipId).Select(row => row.SeasonId).SingleAsync(cancellationToken);
}
