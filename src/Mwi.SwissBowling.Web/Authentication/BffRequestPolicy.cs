namespace Mwi.SwissBowling.Web.Authentication;

public static class BffRequestPolicy
{
    public static bool IsLocalReturnUrl(string? value) =>
        value is not null && value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && !value.Contains('\\');

    public static bool IsAllowedApiPath(string path)
    {
        var parts = path.Split('/');
        return parts[0] is "identity" or "setup" or "events" &&
            !parts.Any(part => part is "." or ".." || part.Contains('\\'));
    }
}
