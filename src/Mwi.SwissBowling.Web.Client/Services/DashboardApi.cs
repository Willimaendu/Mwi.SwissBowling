using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mwi.SwissBowling.Web.Client.Services;

public sealed class DashboardApi(HttpClient http) : IDashboardApi
{
    public async Task<T> ReadAsync<T>(string resource, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync("bff/api/" + resource, cancellationToken);
        await RequireSuccessAsync(response, cancellationToken);
        return await ReadBodyAsync<T>(response, cancellationToken);
    }

    public Task CreateAsync(string resource, object input, CancellationToken cancellationToken = default) =>
        WriteAsync(HttpMethod.Post, resource, input, cancellationToken);

    public Task UpdateAsync(string resource, object input, CancellationToken cancellationToken = default) =>
        WriteAsync(HttpMethod.Put, resource, input, cancellationToken);

    public Task<T> CreateAsync<T>(string resource, object input, CancellationToken cancellationToken = default) =>
        WriteAsync<T>(HttpMethod.Post, resource, input, cancellationToken);

    public Task<T> UpdateAsync<T>(string resource, object input, CancellationToken cancellationToken = default) =>
        WriteAsync<T>(HttpMethod.Put, resource, input, cancellationToken);

    private async Task WriteAsync(HttpMethod method, string resource, object input, CancellationToken cancellationToken)
    {
        using var response = await SendMutationAsync(method, resource, input, cancellationToken);
    }

    private async Task<T> WriteAsync<T>(HttpMethod method, string resource, object input, CancellationToken cancellationToken)
    {
        using var response = await SendMutationAsync(method, resource, input, cancellationToken);
        return await ReadBodyAsync<T>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendMutationAsync(HttpMethod method, string resource, object input, CancellationToken cancellationToken)
    {
        using var proofResponse = await http.GetAsync("bff/antiforgery", cancellationToken);
        await RequireSuccessAsync(proofResponse, cancellationToken);
        var proof = await ReadBodyAsync<AntiforgeryProof>(proofResponse, cancellationToken);
        using var request = new HttpRequestMessage(method, "bff/api/" + resource)
        {
            Content = JsonContent.Create(input, input.GetType())
        };
        request.Headers.Add("X-CSRF-TOKEN", proof.RequestToken);
        var response = await http.SendAsync(request, cancellationToken);
        try { await RequireSuccessAsync(response, cancellationToken); return response; }
        catch { response.Dispose(); throw; }
    }

    private static async Task<T> ReadBodyAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<T>(cancellationToken)
        ?? throw new DashboardApiException(502, "Der Server hat keine gültigen Daten zurückgegeben.");

    private static async Task RequireSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Ihre Sitzung ist abgelaufen. Bitte melden Sie sich erneut an.",
            HttpStatusCode.Forbidden => "Für diese Aktion fehlt die erforderliche Freigabe oder Berechtigung.",
            HttpStatusCode.NotFound => "Der angeforderte Eintrag wurde nicht gefunden.",
            HttpStatusCode.Conflict => "Der Stand wurde zwischenzeitlich geändert oder ist zur Bearbeitung gesperrt.",
            _ => "Der Dienst ist derzeit nicht erreichbar. Ihre Eingaben wurden nicht verworfen."
        };
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            try
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (body.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    message = title.GetString() ?? message;
                if (body.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var details = errors.EnumerateObject().Where(field => field.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(field => field.Value.EnumerateArray()).Where(value => value.ValueKind == JsonValueKind.String)
                        .Select(value => value.GetString()).Where(value => !string.IsNullOrWhiteSpace(value));
                    message = string.Join(" ", details.Prepend(message));
                }
            }
            catch (JsonException) { }
        }
        throw new DashboardApiException((int)response.StatusCode, message);
    }

    private sealed record AntiforgeryProof(string RequestToken);
}
