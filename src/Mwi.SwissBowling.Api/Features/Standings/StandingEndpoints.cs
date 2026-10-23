using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Standings;

public static class StandingEndpoints
{
    public static void MapStandingEndpoints(this IEndpointRouteBuilder endpoints) => endpoints.MapGet("/api/events/{eventId:guid}/standings",
        async (Guid eventId, ApplicationDbContext database, CancellationToken cancellationToken) =>
        {
            if (!await database.Events.AnyAsync(row => row.Id == eventId, cancellationToken)) throw new RequestRejectedException(404, "Die Meisterschaft ist nicht vorhanden.");
            return StandingRules.Calculate(await ResultQueries.ReadAsync(database, eventId, cancellationToken));
        }).RequireAuthorization(ApiIdentity.Administrator);
}
