using MudBlazor;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Core;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players.Admin;

public partial class PlayersAdmin
{
    private MudTable<PlayerDto>? _table;
    private List<ClubDto> _clubs = [];
    private List<AssociationDto> _associations = [];
    private string _search = string.Empty;
    private bool _showInactive;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _associations = await Api.GetAsync<List<AssociationDto>>(AssociationRoutes.Base);
        _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base);
    });

    private async Task<TableData<PlayerDto>> LoadPageAsync(TableState state, CancellationToken ct)
    {
        try
        {
            var filter = new PlayerFilter(_search, IncludeInactive: _showInactive);
            var page = await Api.GetAsync<PagedResult<PlayerDto>>(PlayerRoutes.Search(filter, state.Page + 1, state.PageSize));
            return new TableData<PlayerDto> { Items = page.Items, TotalItems = page.Total };
        }
        catch (ApiException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
            return new TableData<PlayerDto> { Items = [], TotalItems = 0 };
        }
    }

    private Task ReloadAsync() => _table?.ReloadServerData() ?? Task.CompletedTask;

    private async Task EditAsync(PlayerDto? player)
    {
        var parameters = new DialogParameters<PlayerDialog>
        {
            { d => d.Player, player },
            { d => d.Clubs, _clubs },
            { d => d.Associations, _associations }
        };

        if (await ShowDialogAsync<PlayerDialog, PlayerDto>(player is null ? "Neuer Kegler" : "Kegler bearbeiten", parameters) is not null)
        {
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync(PlayerDto player)
    {
        if (await ConfirmAsync("Kegler löschen", $"Soll {player.Name} gelöscht werden? Kegler mit Resultaten können nur deaktiviert werden.")
            && await TryAsync(() => Api.DeleteAsync(PlayerRoutes.ById(player.Id)), "Kegler gelöscht."))
        {
            await ReloadAsync();
        }
    }
}
