using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Core;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players;

public partial class Players
{
    [SupplyParameterFromQuery(Name = "club")]
    public Guid? ClubId { get; set; }

    [SupplyParameterFromQuery(Name = "association")]
    public Guid? AssociationId { get; set; }

    private MudTable<PlayerDto>? _table;
    private List<ClubDto> _clubs = [];
    private ClubDto? _club;
    private string _search = string.Empty;
    private PlayerCategory? _category;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _clubs = await FetchAsync<List<ClubDto>>("clubs", ClubRoutes.Base);
        _club = _clubs.FirstOrDefault(c => c.Id == ClubId);
    });

    /// <summary>Eine Seite vom Server – Filter und Paging laufen in der Datenbank, der Browser hält nur die sichtbare Seite.</summary>
    private async Task<TableData<PlayerDto>> LoadPageAsync(TableState state, CancellationToken ct)
    {
        var filter = new PlayerFilter(_search, _club?.Id, AssociationId, _category);
        var url = PlayerRoutes.Search(filter, state.Page + 1, state.PageSize);
        try
        {
            var page = await FetchAsync<PagedResult<PlayerDto>>($"players:{url}", url);
            return new TableData<PlayerDto> { Items = page.Items, TotalItems = page.Total };
        }
        catch (ApiException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
            return new TableData<PlayerDto> { Items = [], TotalItems = 0 };
        }
    }

    private Task ReloadAsync() => _table?.ReloadServerData() ?? Task.CompletedTask;

    private Task<IEnumerable<ClubDto>> SearchClubsAsync(string? text, CancellationToken ct) =>
        Task.FromResult(_clubs.Where(c => string.IsNullOrWhiteSpace(text)
            || c.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (c.City?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)));
}
