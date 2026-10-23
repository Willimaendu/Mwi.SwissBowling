using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Mwi.SwissBowling.Core.Authentication;

namespace Mwi.SwissBowling.Web.Authentication;

public sealed class WebSessionValidator(PostgresTicketStore tickets, IdentityTokenRefresher refresher, TimeProvider clock) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Properties.ExpiresUtc <= clock.GetUtcNow()) { context.RejectPrincipal(); return; }
        if (IdentityTokenRefresher.TokenExpiry(context.Properties) > clock.GetUtcNow().AddSeconds(60)) return;
        if (!context.Properties.Items.TryGetValue("session-key", out var key) || key is null) { context.RejectPrincipal(); return; }
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.HttpContext.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var ticket = await tickets.RefreshAsync(key, stored => refresher.RefreshAsync(stored, timeout.Token), timeout.Token);
            if (ticket is null) { context.RejectPrincipal(); return; }
            context.ReplacePrincipal(ticket.Principal);
            context.Properties.StoreTokens(ticket.Properties.GetTokens());
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException)
        {
            context.RejectPrincipal();
        }
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }


    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

}
