using MudBlazor;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players.Admin;

public partial class PlayersAdmin
{
    private List<PlayerDto> _players = [];
    private List<ClubDto> _clubs = [];
    private string _search = string.Empty;
    private bool _showInactive;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base);
        await ReloadPlayersAsync();
    });

    private async Task ReloadPlayersAsync() => _players = await Api.GetAsync<List<PlayerDto>>(PlayerRoutes.All(includeInactive: true));

    private bool Matches(PlayerDto player) =>
        (_showInactive || player.IsActive)
        && (string.IsNullOrWhiteSpace(_search)
            || player.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || player.ClubName.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || (player.LicenseNumber?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));

    private async Task EditAsync(PlayerDto? player)
    {
        var parameters = new DialogParameters<PlayerDialog> { { d => d.Player, player }, { d => d.Clubs, _clubs } };
        if (await ShowDialogAsync<PlayerDialog, PlayerDto>(player is null ? "Neuer Kegler" : "Kegler bearbeiten", parameters) is not null)
        {
            await TryAsync(ReloadPlayersAsync);
        }
    }

    private async Task DeleteAsync(PlayerDto player)
    {
        if (await ConfirmAsync("Kegler löschen", $"Soll {player.Name} gelöscht werden? Kegler mit Resultaten können nur deaktiviert werden.")
            && await TryAsync(() => Api.DeleteAsync(PlayerRoutes.ById(player.Id)), "Kegler gelöscht."))
        {
            await TryAsync(ReloadPlayersAsync);
        }
    }
}
