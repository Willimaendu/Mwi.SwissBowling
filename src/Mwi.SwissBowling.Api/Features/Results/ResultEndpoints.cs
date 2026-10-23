using System.Security.Claims;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using Microsoft.EntityFrameworkCore;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Api.Persistence;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Results;

public static class ResultEndpoints
{
    public static void AddResultRecording(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EditingLeaseOptions>().Bind(configuration.GetSection("EditingLeases"))
            .Validate(options => options.DurationSeconds is >= 60 and <= 3600 && options.RenewalSeconds >= 5 && options.RenewalSeconds * 2 < options.DurationSeconds, "Invalid editing lease duration/renewal configuration.").ValidateOnStart();
        services.AddScoped<ResultRecordingService>();
    }
    public static void MapResultEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/events/{eventId:guid}").RequireAuthorization(ApiIdentity.Administrator);
        group.MapGet("/results", async (Guid eventId, ApplicationDbContext database, CancellationToken cancellationToken) =>
        {
            if (!await database.Events.AnyAsync(row => row.Id == eventId, cancellationToken)) throw new RequestRejectedException(404, "Die Meisterschaft ist nicht vorhanden.");
            return await ResultQueries.ReadAsync(database, eventId, cancellationToken);
        });
        group.MapPost("/leases/{playerId:guid}", (Guid eventId, Guid playerId, ClaimsPrincipal user, ResultRecordingService service, CancellationToken cancellationToken) => service.AcquireAsync(eventId, playerId, user, cancellationToken));
        group.MapPut("/leases/{playerId:guid}", (Guid eventId, Guid playerId, LeaseTokenInput input, ClaimsPrincipal user, ResultRecordingService service, CancellationToken cancellationToken) => service.RenewAsync(eventId, playerId, input.Token, user, cancellationToken));
        group.MapPost("/leases/{playerId:guid}/release", async (Guid eventId, Guid playerId, LeaseTokenInput input, ClaimsPrincipal user, ResultRecordingService service, CancellationToken cancellationToken) =>
        {
            await service.ReleaseAsync(eventId, playerId, input.Token, user, cancellationToken);
            return HttpResults.NoContent();
        });
        group.MapPut("/results/{playerId:guid}", async (Guid eventId, Guid playerId, ResultInput input, ClaimsPrincipal user, ResultRecordingService service, CancellationToken cancellationToken) =>
            HttpResults.Ok(new { version = await service.SaveAsync(eventId, playerId, input, user, cancellationToken) }));
    }
}
