using Mwi.SwissBowling.Api.Features.Identity;

namespace Mwi.SwissBowling.Api.Features.Setup;

public static class SetupEndpoints
{
    public static void MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/setup").RequireAuthorization(ApiIdentity.Administrator);
        group.MapGet("", SetupQueries.ReadAsync);
        group.MapPost("/seasons", SetupCommands.CreateSeasonAsync);
        group.MapPost("/clubs", SetupCommands.CreateClubAsync);
        group.MapPost("/players", SetupCommands.CreatePlayerAsync);
        group.MapPut("/players/{id:guid}", SetupCommands.UpdatePlayerAsync);
        group.MapPost("/members", SetupCommands.AssignMemberAsync);
        group.MapPost("/venues", SetupCommands.CreateVenueAsync);
        group.MapPost("/annual-championships", SetupCommands.CreateAnnualAsync);
        group.MapPost("/events", SetupCommands.CreateEventAsync);
    }

}
