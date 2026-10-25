using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Core;
using SwissBowling.Core.Import;

namespace SwissBowling.Client.Features.Import.Admin;

public partial class ImportRunLog : IDisposable
{
    [Inject] private IApiClient Api { get; set; } = null!;

    [Parameter, EditorRequired] public Guid RunId { get; set; }

    /// <summary>Solange der Lauf aktiv ist, wird das Protokoll alle 3 Sekunden nachgeladen.</summary>
    [Parameter] public bool Running { get; set; }

    private MudTable<ImportLogEntryDto>? _table;
    private bool _errorsOnly;
    private Timer? _timer;

    protected override void OnParametersSet()
    {
        if (Running && _timer is null)
        {
            _timer = new Timer(_ => _ = InvokeAsync(ReloadAsync), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }
        else if (!Running && _timer is not null)
        {
            _timer.Dispose();
            _timer = null;
            _ = InvokeAsync(ReloadAsync);
        }
    }

    private async Task<TableData<ImportLogEntryDto>> LoadPageAsync(TableState state, CancellationToken ct)
    {
        try
        {
            var page = await Api.GetAsync<PagedResult<ImportLogEntryDto>>(ImportRoutes.Entries(RunId, _errorsOnly, state.Page + 1, state.PageSize));
            return new TableData<ImportLogEntryDto> { Items = page.Items, TotalItems = page.Total };
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            // Nächster Versuch beim nächsten Tick bzw. Seitenwechsel.
            return new TableData<ImportLogEntryDto> { Items = [], TotalItems = 0 };
        }
    }

    private Task ReloadAsync() => _table?.ReloadServerData() ?? Task.CompletedTask;

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
