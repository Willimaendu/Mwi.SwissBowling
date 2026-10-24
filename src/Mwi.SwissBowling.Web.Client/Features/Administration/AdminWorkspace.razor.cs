using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class AdminWorkspace : ComponentBase
{
    [Inject] private IAdminApi Api { get; set; } = default!;

    private IdentityProfile? _profile;
    private bool _sessionKnown;
    private SetupDto? _setup;
    private bool _loading = true, _busy, _editing, _confirmed;
    private string? _error, _message;
    private string _tab = "Übersicht", _reason = "";
    private long _revision;
    private bool _reloadPending, _synchronizing;
    private Guid _eventId;
    private EventDto? SelectedEvent => _setup?.Events.SingleOrDefault(row => row.Id == _eventId);

    protected override Task OnInitializedAsync() => LoadAsync();
    private void OpenEvent(Guid id) { _eventId = id; _confirmed = false; _tab = "Erfassung"; }
    private Task ReloadAsync() => LoadAsync(showLoading: false);

    private async Task EditingChangedAsync(bool editing)
    {
        _editing = editing;
        if (!editing && _reloadPending) await SynchronizeAsync();
    }

    private async Task SynchronizeAsync()
    {
        _reloadPending = true;
        if (_editing || _busy || _synchronizing) return;
        _synchronizing = true;
        try
        {
            while (_reloadPending && !_editing && !_busy)
            {
                _reloadPending = false;
                await LoadAsync(showLoading: false);
                _revision++;
            }
        }
        finally { _synchronizing = false; }
    }

    private static string StateName(EventState state) => state switch
    {
        EventState.Published => "Veröffentlicht",
        EventState.Correcting => "In Korrektur",
        _ => "Entwurf"
    };

    private Task LoadAsync() => LoadAsync(showLoading: true);
    private async Task LoadAsync(bool showLoading)
    {
        if (showLoading) _loading = true;
        _error = null;
        try
        {
            _profile = await Api.GetAsync<IdentityProfile>("identity/me");
            _sessionKnown = true;
            if (_profile.IsAdministrator) _setup = await Api.GetAsync<SetupDto>("setup");
        }
        catch (ApiException exception)
        {
            if (exception.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _sessionKnown = true;
                _profile = null;
                _setup = null;
            }
            else _error = exception.Message;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Keine Verbindung. Bitte versuchen Sie es erneut."; }
        finally { _loading = false; }
    }

    private Task SelectEventAsync(ChangeEventArgs args)
    {
        _eventId = Guid.TryParse(args.Value?.ToString(), out var id) ? id : Guid.Empty;
        _confirmed = false;
        _message = null;
        return Task.CompletedTask;
    }

    private Task RegisterAsync() => ExecuteAsync(async () =>
    {
        await Api.PostAsync("identity/register", new { });
        _message = "Ihre Anfrage wurde registriert.";
        await LoadAsync();
    });

    private Task PublishAsync() => ExecuteAsync(async () =>
    {
        await Api.PostAsync($"events/{_eventId}/finalize", new { });
        _confirmed = false;
        _message = "Die Rangliste ist veröffentlicht.";
        await LoadAsync();
    });

    private Task RequestCorrectionAsync() => ExecuteAsync(async () =>
    {
        await Api.PostAsync($"events/{_eventId}/corrections", new CorrectionInput(_reason));
        _reason = "";
        _message = "Die Korrekturanfrage wartet auf eine zweite Person.";
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        _busy = true;
        _error = null;
        _message = null;
        try { await action(); }
        catch (ApiException exception) { _error = exception.Message; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Verbindung unterbrochen. Der Ausgang ist unklar; bitte vor erneutem Absenden den Stand aktualisieren."; }
        finally
        {
            _busy = false;
            if (_reloadPending) await SynchronizeAsync();
        }
    }
}
