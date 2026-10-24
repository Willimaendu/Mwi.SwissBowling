using MudBlazor;
using SwissBowling.Core.Clubs;

namespace SwissBowling.Client.Features.Clubs.Admin;

public partial class ClubsAdmin
{
    private List<ClubDto> _clubs = [];

    protected override Task OnInitializedAsync() => ReloadAsync();

    private Task ReloadAsync() => LoadAsync(async () => _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base));

    private async Task EditAsync(ClubDto? club)
    {
        var parameters = new DialogParameters<ClubDialog> { { d => d.Club, club } };
        if (await ShowDialogAsync<ClubDialog, ClubDto>(club is null ? "Neuer Klub" : "Klub bearbeiten", parameters) is not null)
        {
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync(ClubDto club)
    {
        if (await ConfirmAsync("Klub löschen", $"Soll der Klub «{club.Name}» gelöscht werden?")
            && await TryAsync(() => Api.DeleteAsync(ClubRoutes.ById(club.Id)), "Klub gelöscht."))
        {
            await ReloadAsync();
        }
    }
}
