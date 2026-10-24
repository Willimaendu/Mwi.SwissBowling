using SwissBowling.Client.Shared.Services;
using SwissBowling.Core.Import;

namespace SwissBowling.Client.Features.Import.Admin;

public partial class UvwebImport : IDisposable
{
    private List<ImportRunDto> _runs = [];
    private bool _includeArchive = true;
    private string? _startError;
    private Timer? _timer;

    private bool IsRunning => _runs.Any(r => r.Status == ImportRunStatus.Running);

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync(ReloadAsync);
        _timer = new Timer(_ => _ = PollAsync(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
    }

    private async Task ReloadAsync() => _runs = await Api.GetAsync<List<ImportRunDto>>(ImportRoutes.Runs);

    /// <summary>Solange ein Lauf aktiv ist, das Protokoll nachladen.</summary>
    private async Task PollAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            await ReloadAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException)
        {
            // Nächster Versuch beim nächsten Tick.
        }
    }

    private async Task StartAsync()
    {
        _startError = null;
        try
        {
            var run = await Api.PostAsync<ImportRunDto>(ImportRoutes.Runs, new StartImportRequest(_includeArchive));
            _runs.Insert(0, run);
        }
        catch (ApiException ex)
        {
            _startError = ex.Message;
        }
    }

    public void Dispose() => _timer?.Dispose();
}
