using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Web.Client.Features.Standings;

public partial class PublishedRanking : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter, EditorRequired] public Guid EventId { get; set; }
    [Parameter] public PublicEventDto? InitialEvent { get; set; }
    [Parameter] public bool InitialUnavailable { get; set; }
    [Parameter] public bool InitialNotFound { get; set; }

    private PublicEventDto? _event;
    private string _search = "";
    private string? _error;
    private bool _notFound;

    protected override void OnInitialized()
    {
        _event = InitialEvent;
        _notFound = InitialNotFound;
        if (InitialUnavailable) _error = "Rangliste derzeit nicht erreichbar. Die Verbindung wird automatisch erneut geprüft.";
    }

    private static string SwissTime(DateTimeOffset time) => TimeZoneInfo.ConvertTime(time, TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich")).ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("de-CH"));
    private bool Matches(RankedRow row) => $"{row.Name} {row.Club}".Contains(_search, StringComparison.CurrentCultureIgnoreCase);

    private async Task LoadAsync()
    {
        try
        {
            using var client = new HttpClient { BaseAddress = Navigation.ToAbsoluteUri("/") };
            using var response = await client.GetAsync($"public-api/events/{EventId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _event = null;
                _notFound = true;
                _error = null;
                return;
            }
            response.EnsureSuccessStatusCode();
            _event = await response.Content.ReadFromJsonAsync<PublicEventDto>();
            _notFound = _event is null;
            _error = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Aktualisierung derzeit nicht möglich. Der letzte veröffentlichte Stand bleibt sichtbar."; }
    }
}
