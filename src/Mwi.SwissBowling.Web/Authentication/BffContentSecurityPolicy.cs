namespace Mwi.SwissBowling.Web.Authentication;

public static class BffContentSecurityPolicy
{
    public static string Create(string? authority)
    {
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("A valid HTTP(S) authentication authority without user information is required.");

        // Browsers also apply form-action to the cross-origin OIDC logout redirect.
        var identityOrigin = uri.GetLeftPart(UriPartial.Authority);
        return $"default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self' {identityOrigin}";
    }
}
