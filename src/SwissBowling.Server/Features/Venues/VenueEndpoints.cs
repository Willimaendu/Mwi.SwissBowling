using Microsoft.EntityFrameworkCore;
using SwissBowling.Core.Venues;
using SwissBowling.Server.Data;

namespace SwissBowling.Server.Features.Venues;

public static class VenueEndpoints
{
    public static void MapVenues(this IEndpointRouteBuilder app)
    {
        app.MapGet(VenueRoutes.Base, GetAll).WithTags("Kegelbahnen");
    }

    private static async Task<IResult> GetAll(AppDbContext db, CancellationToken ct)
    {
        var venues = await db.Venues.AsNoTracking().Include(v => v.Association).OrderBy(v => v.Association.Name).ThenBy(v => v.Zip).ToListAsync(ct);
        return TypedResults.Ok(venues.Select(v => v.ToDto()));
    }
}
