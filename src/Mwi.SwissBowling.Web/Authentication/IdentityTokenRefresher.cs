using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Mwi.SwissBowling.Web.Authentication;

public sealed class IdentityTokenRefresher(IHttpClientFactory clients, IOptionsMonitor<OpenIdConnectOptions> options, TimeProvider clock)
{
    public static DateTimeOffset TokenExpiry(AuthenticationProperties properties) => DateTimeOffset.TryParse(properties.GetTokenValue("expires_at"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires) ? expires : DateTimeOffset.MinValue;

    public async Task<bool> RefreshAsync(AuthenticationTicket stored, CancellationToken cancellationToken)
    {
        if (TokenExpiry(stored.Properties) > clock.GetUtcNow().AddSeconds(60)) return true;
        var refreshToken = stored.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return false;
        var configuration = options.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var metadata = await configuration.ConfigurationManager!.GetConfigurationAsync(cancellationToken);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken,
            ["client_id"] = configuration.ClientId!, ["client_secret"] = configuration.ClientSecret!
        });
        using var response = await clients.CreateClient("identity").PostAsync(metadata.TokenEndpoint, form, cancellationToken);
        if (!response.IsSuccessStatusCode) return false;
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
        if (payload is null || !payload.RootElement.TryGetProperty("access_token", out var accessToken) || accessToken.GetString() is not { Length: > 0 } access) return false;
        var tokens = stored.Properties.GetTokens().ToDictionary(token => token.Name, token => token.Value);
        tokens["access_token"] = access;
        tokens["expires_at"] = clock.GetUtcNow().AddSeconds(payload.RootElement.GetProperty("expires_in").GetInt32()).ToString("O", CultureInfo.InvariantCulture);
        if (payload.RootElement.TryGetProperty("refresh_token", out var next) && next.GetString() is { Length: > 0 } refresh) tokens["refresh_token"] = refresh;
        if (payload.RootElement.TryGetProperty("id_token", out var identity) && identity.GetString() is { Length: > 0 } id) tokens["id_token"] = id;
        stored.Properties.StoreTokens(tokens.Select(token => new AuthenticationToken { Name = token.Key, Value = token.Value }));
        return true;
    }
}
