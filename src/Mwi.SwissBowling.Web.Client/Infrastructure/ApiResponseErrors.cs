using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mwi.SwissBowling.Web.Client.Infrastructure;

public static class ApiResponseErrors
{
    public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized) throw new ApiException("Ihre Sitzung ist abgelaufen. Eingaben bleiben sichtbar; bitte erneut anmelden.", response.StatusCode);
        if (response.StatusCode == HttpStatusCode.Forbidden) throw new ApiException("Ihr Konto hat für diese Aktion keine Freigabe oder Berechtigung.", response.StatusCode);
        ApiProblem? problem = null;
        try { problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken); }
        catch (JsonException) { problem = null; }
        var errors = problem?.Errors?.SelectMany(pair => pair.Value).ToArray();
        throw new ApiException(errors is { Length: > 0 } ? string.Join(" ", errors) : problem?.Title ?? "Die Anfrage konnte nicht verarbeitet werden.", response.StatusCode);
    }

    private sealed record ApiProblem(string? Title, Dictionary<string, string[]>? Errors);
}
