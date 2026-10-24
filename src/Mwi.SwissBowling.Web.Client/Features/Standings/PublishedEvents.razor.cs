using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Standings;

public partial class PublishedEvents : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public PublicEventSummary[] InitialEvents { get; set; } = [];
    [Parameter] public bool InitialUnavailable { get; set; }

    private PublicEventSummary[] _events = [];
    private string? _error;

    protected override void OnInitialized()
    {
        _events = InitialEvents;
        if (InitialUnavailable) _error = "Die Ranglisten sind derzeit nicht erreichbar. Die Verbindung wird automatisch erneut geprüft.";
    }

    private async Task LoadAsync()
    {
        try
        {
            using var client = new HttpClient { BaseAddress = Navigation.ToAbsoluteUri("/") };
            _events = await client.GetFromJsonAsync<PublicEventSummary[]>("public-api/events") ?? [];
            _error = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Aktualisierung derzeit nicht möglich. Der letzte Stand bleibt sichtbar; wir versuchen es bei der nächsten Änderung erneut."; }
    }
}
