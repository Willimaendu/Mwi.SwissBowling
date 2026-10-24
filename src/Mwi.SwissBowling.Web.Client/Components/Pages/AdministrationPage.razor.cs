using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.FluentUI.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Services;

namespace Mwi.SwissBowling.Web.Client.Components.Pages;

public partial class AdministrationPage : ComponentBase
{
    [Inject] private IDashboardApi Api { get; set; } = default!;
    [Parameter] public string Section { get; set; } = "overview";

    private readonly PaginationState _pagination = new() { ItemsPerPage = 20 };
    private IdentityProfile? _profile;
    private SetupDto? _setup;
    private bool _loading = true, _requiresLogin, _busy, _editing, _refreshing, _pendingRefresh;
    private string? _problem, _message;
    private long _revision;

    protected override Task OnInitializedAsync() => SynchronizeAsync();
    internal static string StateLabel(EventState state) => state switch
    {
        EventState.Published => "Veröffentlicht",
        EventState.Correcting => "In Korrektur",
        _ => "Entwurf"
    };

    private async Task SynchronizeAsync()
    {
        _pendingRefresh = true;
        if (_editing || _busy || _refreshing) return;
        _refreshing = true;
        try
        {
            while (_pendingRefresh && !_editing && !_busy)
            {
                _pendingRefresh = false;
                _problem = null;
                try
                {
                    var profile = await Api.ReadAsync<IdentityProfile>("identity/me");
                    var setup = profile.IsAdministrator ? await Api.ReadAsync<SetupDto>("setup") : null;
                    _profile = profile;
                    _setup = setup;
                    _requiresLogin = false;
                    _revision++;
                }
                catch (DashboardApiException failure) when (failure.StatusCode == 401)
                { _profile = null; _setup = null; _requiresLogin = true; }
                catch (DashboardApiException failure) { _problem = failure.Message; }
                catch (Exception failure) when (failure is HttpRequestException or OperationCanceledException)
                { _problem = "Der Dienst ist derzeit nicht erreichbar. Der letzte geladene Stand bleibt erhalten."; }
                finally { _loading = false; }
            }
        }
        finally { _refreshing = false; }
    }

    private void ProtectEditing(LocationChangingContext navigation)
    {
        if (!_editing) return;
        navigation.PreventNavigation();
        _problem = "Bitte speichern oder verwerfen Sie die laufende Resultaterfassung vor dem Seitenwechsel.";
    }

    private async Task EditingChangedAsync(bool editing)
    {
        _editing = editing;
        if (!editing && _pendingRefresh) await SynchronizeAsync();
    }

    private async Task RegisterAsync()
    {
        _busy = true;
        _problem = null;
        try { await Api.CreateAsync("identity/register", new { }); _message = "Die Freigabe wurde beantragt."; }
        catch (DashboardApiException failure) { _problem = failure.Message; }
        catch (Exception failure) when (failure is HttpRequestException or OperationCanceledException)
        { _problem = "Die Registrierung konnte nicht bestätigt werden. Der Status wird automatisch geprüft."; }
        finally { _busy = false; await SynchronizeAsync(); }
    }
}
