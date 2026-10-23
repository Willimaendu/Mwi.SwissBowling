using System.Net;
using System.Net.Http.Json;

namespace Mwi.SwissBowling.Web.Client.Infrastructure;

public sealed class AdminApi(HttpClient client) : IAdminApi
{
    public async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("bff/api/" + path, cancellationToken);
        await ApiResponseErrors.EnsureSuccessAsync(response, cancellationToken);
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
        await ApiResponseErrors.EnsureSuccessAsync(tokenResponse, cancellationToken);
        var token = await tokenResponse.Content.ReadFromJsonAsync<CsrfToken>(cancellationToken) ?? throw new ApiException("Sicherheitsnachweis fehlt. Bitte erneut anmelden.");
        using var request = new HttpRequestMessage(method, "bff/api/" + path) { Content = JsonContent.Create(body, body.GetType()) };
        request.Headers.Add("X-CSRF-TOKEN", token.RequestToken);
        var response = await client.SendAsync(request, cancellationToken);
        try { await ApiResponseErrors.EnsureSuccessAsync(response, cancellationToken); return response; }
        catch { response.Dispose(); throw; }
    }
    private sealed record CsrfToken(string RequestToken);
}
public sealed class ApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
