using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class ResultEditor : ComponentBase, IAsyncDisposable
{
    [Inject] private IAdminApi Api { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public SetupDto Data { get; set; } = default!;
    [Parameter] public EventCallback<bool> EditingChanged { get; set; }
    [Parameter] public long Revision { get; set; }

    private long _loadedRevision = -1;
    private ResultDto[] _results = [];
    private Guid _playerId;
    private EditingLeaseSession? _session;
    private LeaseDto? _lease => _session?.Lease;
    private bool _leaseValid => _session?.IsValid == true;
    private bool _busy, _interrupted;
    private int _total, _nines, _eights;
    private long _version;
    private string _search = "";
    private string? _error, _message;
    private IEnumerable<PlayerDto> EligiblePlayers
    {
        get
        {
            var season = Data.AnnualChampionships.Single(row => row.Id == Event.AnnualChampionshipId).SeasonId;
            var ids = Data.Members.Where(row => row.SeasonId == season).Select(row => row.PlayerId).ToHashSet();
            return Data.Players.Where(row => ids.Contains(row.Id) && $"{row.LastName} {row.FirstName}".Contains(_search, StringComparison.OrdinalIgnoreCase));
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedRevision == Revision || _lease is not null || _busy) return;
        _loadedRevision = Revision;
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try { _results = await Api.GetAsync<ResultDto[]>($"events/{Event.Id}/results"); }
        catch (ApiException exception) { _error = exception.Message; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Resultate konnten nicht geladen werden."; }
    }

    private async Task AcquireAsync()
    {
        _busy = true; _error = null; _message = null;
        try
        {
            _session = new EditingLeaseSession(Api, Event.Id, _playerId, TimeProvider.System)
            {
                RenewalFailed = () => InvokeAsync(() =>
                {
                    _error = "Die Bearbeitungssperre konnte nicht verlängert werden. Eingaben bleiben sichtbar. Sperre freigeben und gespeicherten Stand prüfen, bevor Sie erneut bearbeiten.";
                    StateHasChanged();
                })
            };
            await _session.AcquireAsync();
            await EditingChanged.InvokeAsync(true);
            _results = await Api.GetAsync<ResultDto[]>($"events/{Event.Id}/results");
            var current = _results.SingleOrDefault(row => row.PlayerId == _playerId);
            _total = current?.Total ?? 0;
            _nines = current?.Nines ?? 0;
            _eights = current?.Eights ?? 0;
            _interrupted = current?.Interrupted ?? false;
            _version = current?.Version ?? 0;
        }
        catch (ApiException exception) { _session?.Invalidate(); _error = exception.Message; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _session?.Invalidate(); _error = "Reservierung oder Laden fehlgeschlagen. Vor einer neuen Bearbeitung Sperre freigeben oder ablaufen lassen."; }
        finally { _busy = false; }
    }

    private async Task SaveAsync()
    {
        if (_lease is null || !_leaseValid) return;
        _busy = true; _error = null; _message = null;
        try
        {
            await _session!.StopRenewalAsync();
            if (!_session.IsValid) return;
            var saved = await Api.PutAsync<SavedVersion>($"events/{Event.Id}/results/{_playerId}", new ResultInput(_total, _nines, _eights, _interrupted, _version, _lease.Token));
            _version = saved.Version;
            await _session.ReleaseAsync();
            await EditingChanged.InvokeAsync(false);
            _message = "Resultat gespeichert und Bearbeitung freigegeben.";
            await RefreshAsync();
        }
        catch (ApiException exception)
        { _session?.Invalidate(); _error = exception.Message + " Eingaben bleiben sichtbar. Vor weiterer Bearbeitung Sperre freigeben und gespeicherten Stand prüfen."; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _session?.Invalidate(); _error = "Verbindung unterbrochen: Speicherstand unklar. Nicht blind erneut speichern. Sperre freigeben und Resultat aktualisieren; Eingaben bleiben bis dahin sichtbar."; }
        finally { _busy = false; }
    }

    private async Task ReleaseAsync()
    {
        if (_lease is null) return;
        _busy = true;
        try
        {
            await _session!.ReleaseAsync();
            await ClearLeaseAsync();
        }
        catch (ApiException exception) { _error = exception.Message; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Freigabe nicht bestätigt. Bitte erneut versuchen; die Sperre läuft serverseitig ab."; }
        finally { _session?.Invalidate(); _busy = false; }
    }

    private async Task ClearLeaseAsync()
    {
        _error = null;
        await EditingChanged.InvokeAsync(false);
        await RefreshAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_session is not null) await _session.DisposeAsync();
    }

    private sealed record SavedVersion(long Version);
}
