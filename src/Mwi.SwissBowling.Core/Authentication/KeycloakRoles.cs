using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Mwi.SwissBowling.Core.Authentication;

public sealed class KeycloakRoles : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated) return Task.FromResult(principal);
        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess is null) return Task.FromResult(principal);
        try
        {
            using var document = JsonDocument.Parse(realmAccess);
            if (document.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                foreach (var role in roles.EnumerateArray())
                    if (role.ValueKind == JsonValueKind.String && role.GetString() is { } name && !identity.HasClaim(identity.RoleClaimType, name))
                        identity.AddClaim(new Claim(identity.RoleClaimType, name));
        }
        catch (JsonException) { return Task.FromResult(principal); }
        return Task.FromResult(principal);
    }
}
