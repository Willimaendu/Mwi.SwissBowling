using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Services;

/// <summary>Schlanker, typisierter Zugriff auf die REST API. Die Routen liefern die Feature-Verträge in Core.</summary>
public interface IApiClient
{
    Task<T> GetAsync<T>(string url, CancellationToken ct = default);

    Task<T> PostAsync<T>(string url, object? body, CancellationToken ct = default);

    Task<T> PutAsync<T>(string url, object body, CancellationToken ct = default);

    Task DeleteAsync(string url, CancellationToken ct = default);
}

public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>
/// Läuft in allen Render-Modi (Prerendering, Interactive Server, WebAssembly): Die Basisadresse stammt aus dem
/// <see cref="NavigationManager"/>, das JWT pro Anfrage aus dem benutzerbezogenen <see cref="ITokenStore"/>.
/// </summary>
public sealed class ApiClient(HttpClient http, NavigationManager navigation, ITokenStore tokens) : IApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<T> GetAsync<T>(string url, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Get, url, null, ct);

    public Task<T> PostAsync<T>(string url, object? body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Post, url, body ?? new { }, ct);

    public Task<T> PutAsync<T>(string url, object body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Put, url, body, ct);

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        using var response = await SendRawAsync(HttpMethod.Delete, url, null, ct);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var response = await SendRawAsync(method, url, body, ct);
        if (typeof(T) == typeof(Unit))
        {
            return (T)(object)Unit.Value;
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, ct) ?? throw new ApiException(response.StatusCode, "Leere Antwort der API.");
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(new Uri(navigation.BaseUri), url));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        if (await tokens.GetAsync() is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var message = await ReadProblemAsync(response, ct);
        response.Dispose();
        throw new ApiException(response.StatusCode, message);
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>(Json, ct);
            return problem?.Detail
                ?? (problem?.Errors is { Count: > 0 } errors ? string.Join(" ", errors.SelectMany(e => e.Value)) : null)
                ?? problem?.Title
                ?? $"Fehler {(int)response.StatusCode}";
        }
        catch (JsonException)
        {
            return $"Fehler {(int)response.StatusCode}";
        }
    }

    private sealed record Problem(string? Title, string? Detail, Dictionary<string, string[]>? Errors);
}

/// <summary>Rückgabetyp für Aufrufe ohne Antwort-Body (204).</summary>
public readonly record struct Unit
{
    public static readonly Unit Value;
}
