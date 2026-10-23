using System.Net;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Features.Standings;

public sealed class PublicEventsClient(IHttpClientFactory clients)
{
    public async Task<PublicEventSummary[]> ListAsync(CancellationToken cancellationToken = default) =>
        await clients.CreateClient("api").GetFromJsonAsync<PublicEventSummary[]>("api/public/events", cancellationToken) ?? [];

    public async Task<PublicEventDto?> ReadAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using var response = await clients.CreateClient("api").GetAsync($"api/public/events/{eventId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PublicEventDto>(cancellationToken);
    }
}
