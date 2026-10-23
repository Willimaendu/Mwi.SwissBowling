using System.Net.Http.Headers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Mwi.SwissBowling.Web.Authentication;

public static class BffEndpoints
{
    public static void MapBffEndpoints(this WebApplication app)
    {
        app.MapGet("/public-api/events", async (IHttpClientFactory clients, CancellationToken cancellationToken) =>
            Results.Ok(await clients.CreateClient("api").GetFromJsonAsync<Mwi.SwissBowling.Contracts.PublicEventSummary[]>("api/public/events", cancellationToken)));
        app.MapGet("/public-api/events/{eventId:guid}", async (Guid eventId, Mwi.SwissBowling.Web.Features.Standings.PublicEventsClient events, CancellationToken cancellationToken) =>
            await events.ReadAsync(eventId, cancellationToken) is { } result ? Results.Ok(result) : Results.NotFound());
        app.MapGet("/account/login", (string? returnUrl) => Results.Challenge(
            new AuthenticationProperties { RedirectUri = BffRequestPolicy.IsLocalReturnUrl(returnUrl) ? returnUrl : "/administration" }, [OpenIdConnectDefaults.AuthenticationScheme]));
        app.MapPost("/account/logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            return Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        }).RequireAuthorization("BffSession");
        app.MapGet("/bff/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { requestToken = antiforgery.GetAndStoreTokens(context).RequestToken });
        }).RequireAuthorization("BffSession");
        app.MapMethods("/bff/api/{**path}", ["GET", "POST", "PUT", "DELETE"], RelayAsync).RequireAuthorization("BffSession");
    }

    private static async Task RelayAsync(string path, HttpContext context, IAntiforgery antiforgery, IHttpClientFactory clients)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!BffRequestPolicy.IsAllowedApiPath(path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        if (context.Request.Method != "GET")
        {
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
        }
        var token = await context.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(token)) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), $"api/{path}{context.Request.QueryString}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (context.Request.Method is "POST" or "PUT")
        {
            request.Content = new StreamContent(context.Request.Body);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        }
        using var response = await clients.CreateClient("api").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
