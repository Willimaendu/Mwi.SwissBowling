using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using SwissBowling.Core.Auth;

namespace SwissBowling.Client.Shared.Services;

/// <summary>Leitet den Anmeldezustand aus dem gespeicherten JWT ab (Prüfung erfolgt serverseitig).</summary>
public sealed class JwtAuthenticationStateProvider(ITokenStore tokens, IApiClient api) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokens.GetAsync();
        var principal = token is null ? null : Parse(token);
        if (principal is null && token is not null)
        {
            await tokens.SetAsync(null);
        }

        return principal is null ? Anonymous : new AuthenticationState(principal);
    }

    public async Task<UserDto> LoginAsync(LoginRequest request)
    {
        var response = await api.PostAsync<LoginResponse>(AuthRoutes.Login, request);
        await tokens.SetAsync(response.AccessToken);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return response.User;
    }

    public async Task LogoutAsync()
    {
        await tokens.SetAsync(null);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static ClaimsPrincipal? Parse(string token)
    {
        try
        {
            var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var json = JsonDocument.Parse(Convert.FromBase64String(payload));

            if (json.RootElement.TryGetProperty("exp", out var exp) && DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            var claims = json.RootElement.EnumerateObject()
                .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array ? p.Value.EnumerateArray().Select(v => (p.Name, v.ToString())) : [(p.Name, p.Value.ToString())])
                .Select(c => new Claim(c.Name == "role" ? ClaimTypes.Role : c.Name == "email" ? ClaimTypes.Name : c.Name, c.Item2));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role));
        }
        catch (Exception ex) when (ex is FormatException or JsonException or IndexOutOfRangeException)
        {
            return null;
        }
    }
}
