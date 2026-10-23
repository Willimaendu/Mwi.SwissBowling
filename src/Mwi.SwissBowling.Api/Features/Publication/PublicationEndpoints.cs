using System.Security.Claims;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Contracts;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Mwi.SwissBowling.Api.Features.Publication;

public static class PublicationEndpoints
{
    public static void MapPublicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var events = endpoints.MapGroup("/api/events").RequireAuthorization(ApiIdentity.Administrator);
        events.MapGet("/corrections/pending", (PublicationQueries queries, CancellationToken cancellationToken) => queries.PendingAsync(cancellationToken));
        events.MapPost("/{eventId:guid}/finalize", (Guid eventId, ClaimsPrincipal user, PublicationService service, CancellationToken cancellationToken) => service.PublishAsync(eventId, user, cancellationToken));
        events.MapPost("/{eventId:guid}/corrections", async (Guid eventId, CorrectionInput input, ClaimsPrincipal user, PublicationService service, CancellationToken cancellationToken) =>
            new CreatedId(await service.RequestCorrectionAsync(eventId, input.Reason, user, cancellationToken)));
        events.MapPost("/{eventId:guid}/corrections/{requestId:guid}/approve", async (Guid eventId, Guid requestId, ClaimsPrincipal user, PublicationService service, CancellationToken cancellationToken) =>
        {
            await service.ApproveCorrectionAsync(eventId, requestId, user, cancellationToken);
            return HttpResults.NoContent();
        });
        var publicEvents = endpoints.MapGroup("/api/public/events").AllowAnonymous();
        publicEvents.MapGet("", (HttpContext context, PublicationQueries queries, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=0, must-revalidate";
            return queries.ListPublicAsync(cancellationToken);
        });
        publicEvents.MapGet("/{eventId:guid}", async (Guid eventId, HttpContext context, PublicationQueries queries, CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "public, max-age=0, must-revalidate";
            var snapshot = await queries.ReadPublicAsync(eventId, cancellationToken);
            return snapshot is null ? HttpResults.NotFound() : HttpResults.Ok(snapshot);
        });
    }
}
