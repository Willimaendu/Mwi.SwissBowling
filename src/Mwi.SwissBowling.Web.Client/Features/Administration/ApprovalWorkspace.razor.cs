using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class ApprovalWorkspace : ComponentBase
{
    [Inject] private IAdminApi Api { get; set; } = default!;
    [Parameter, EditorRequired] public SetupDto Data { get; set; } = default!;
    [Parameter] public long Revision { get; set; }

    private long _loadedRevision = -1;
    private PendingAccount[] _accounts = [];
    private CorrectionDto[] _corrections = [];
    private readonly Dictionary<Guid, bool> _verified = [];
    private readonly Dictionary<Guid, Guid> _links = [];
    private bool _busy;
    private string? _error, _message;

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedRevision == Revision || _busy) return;
        _loadedRevision = Revision;
        await LoadAsync();
    }

    private Task LoadAsync() => ExecuteAsync(async () =>
    {
        _accounts = await Api.GetAsync<PendingAccount[]>("identity/pending");
        foreach (var account in _accounts) { _verified.TryAdd(account.Id, false); _links.TryAdd(account.Id, Guid.Empty); }
        _corrections = await Api.GetAsync<CorrectionDto[]>("events/corrections/pending");
    });

    private Task ApproveAccountAsync(Guid id) => ExecuteAsync(async () =>
    {
        await Api.PostAsync($"identity/{id}/approve", new ApproveAccountRequest(_verified[id], _links[id] == Guid.Empty ? null : _links[id]));
        _accounts = _accounts.Where(row => row.Id != id).ToArray();
        _message = "Konto freigegeben.";
    });

    private Task ApproveCorrectionAsync(CorrectionDto correction) => ExecuteAsync(async () =>
    {
        await Api.PostAsync($"events/{correction.EventId}/corrections/{correction.Id}/approve", new { });
        _corrections = _corrections.Where(row => row.Id != correction.Id).ToArray();
        _message = "Wiedereröffnung genehmigt. Der Arbeitsbereich wird automatisch synchronisiert.";
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        _busy = true;
        _error = null;
        try { await action(); }
        catch (ApiException exception) { _error = exception.Message; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Verbindung unterbrochen. Bitte vor erneutem Absenden den Stand aktualisieren."; }
        finally
        {
            _busy = false;
            if (_loadedRevision != Revision) { _loadedRevision = Revision; await LoadAsync(); }
        }
    }
}
