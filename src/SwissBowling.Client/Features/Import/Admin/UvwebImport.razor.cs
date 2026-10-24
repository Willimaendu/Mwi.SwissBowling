using SwissBowling.Client.Shared.Services;
using SwissBowling.Core.Import;

namespace SwissBowling.Client.Features.Import.Admin;

public partial class UvwebImport
{
    private List<UvwebChampionshipDto> _current = [];
    private List<UvwebAssociationDto> _associations = [];
    private List<UvwebArchiveEntryDto> _archive = [];
    private readonly List<ImportSummaryDto> _summaries = [];
    private int? _archiveUvId;
    private bool _archiveLoading;
    private string? _archiveError;
    private int? _busy;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _current = await Api.GetAsync<List<UvwebChampionshipDto>>(ImportRoutes.Available);
        _associations = await Api.GetAsync<List<UvwebAssociationDto>>(ImportRoutes.Associations);
    });

    private Task ReloadCurrentAsync() => LoadAsync(async () => _current = await Api.GetAsync<List<UvwebChampionshipDto>>(ImportRoutes.Available));

    private async Task LoadArchiveAsync(int? uvId)
    {
        _archiveUvId = uvId;
        if (uvId is null)
        {
            _archive = [];
            return;
        }

        _archiveLoading = true;
        _archiveError = null;
        try
        {
            _archive = await Api.GetAsync<List<UvwebArchiveEntryDto>>(ImportRoutes.ArchiveOf(uvId.Value));
        }
        catch (ApiException ex)
        {
            _archiveError = ex.Message;
        }
        finally
        {
            _archiveLoading = false;
        }
    }

    private Task ImportCurrentAsync(UvwebChampionshipDto item) => RunImportAsync(item.MsId, item.Name, async () =>
    {
        var summary = await Api.PostAsync<ImportSummaryDto>(ImportRoutes.Championship(item.MsId, item.UvId, item.RanglisteId), null);
        _current = _current.Select(c => c.MsId == item.MsId ? c with { IsImported = true } : c).ToList();
        return summary;
    });

    private Task ImportArchiveAsync(UvwebArchiveEntryDto item) => RunImportAsync(item.MsId, item.Name, async () =>
    {
        var summary = await Api.PostAsync<ImportSummaryDto>(ImportRoutes.Archive, new ImportArchiveRequest(item.MsId, item.UvId, item.PdfUrl));
        _archive = _archive.Select(a => a.MsId == item.MsId ? a with { IsImported = true } : a).ToList();
        return summary;
    });

    private async Task RunImportAsync(int msId, string name, Func<Task<ImportSummaryDto>> import)
    {
        _busy = msId;
        try
        {
            await TryAsync(async () => _summaries.Insert(0, await import()), $"«{name}» importiert.");
        }
        finally
        {
            _busy = null;
        }
    }

    private async Task ImportAllAsync<T>(IReadOnlyList<T> items, Func<T, Task> import)
    {
        foreach (var item in items)
        {
            await import(item);
            StateHasChanged();
        }
    }
}
