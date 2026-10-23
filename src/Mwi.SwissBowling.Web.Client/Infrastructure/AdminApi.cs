using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mwi.SwissBowling.Web.Client.Infrastructure;

public sealed class AdminApi(HttpClient client)
{
    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("bff/api/" + path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new ApiException("Die Serverantwort ist leer.");
    }
    public Task PostAsync(string path, object body, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Post, path, body, cancellationToken);
    public Task PutAsync(string path, object body, CancellationToken cancellationToken = default) => SendAsync(HttpMethod.Put, path, body, cancellationToken);
    public async Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        using var response = await MutationAsync(HttpMethod.Post, path, body, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new ApiException("Die Serverantwort ist leer.");
    }
    public async Task<T> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default)
    {
        using var response = await MutationAsync(HttpMethod.Put, path, body, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken) ?? throw new ApiException("Die Serverantwort ist leer.");
    }
    private async Task SendAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        using var response = await MutationAsync(method, path, body, cancellationToken);
    }
    private async Task<HttpResponseMessage> MutationAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        using var tokenResponse = await client.GetAsync("bff/antiforgery", cancellationToken);
        await EnsureSuccessAsync(tokenResponse, cancellationToken);
        var token = await tokenResponse.Content.ReadFromJsonAsync<CsrfToken>(cancellationToken) ?? throw new ApiException("Sicherheitsnachweis fehlt. Bitte erneut anmelden.");
        using var request = new HttpRequestMessage(method, "bff/api/" + path) { Content = JsonContent.Create(body, body.GetType()) };
        request.Headers.Add("X-CSRF-TOKEN", token.RequestToken);
        var response = await client.SendAsync(request, cancellationToken);
        try { await EnsureSuccessAsync(response, cancellationToken); return response; }
        catch { response.Dispose(); throw; }
    }
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
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
    private sealed record CsrfToken(string RequestToken);
    private sealed record ApiProblem(string? Title, Dictionary<string, string[]>? Errors);
}
public sealed class ApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
