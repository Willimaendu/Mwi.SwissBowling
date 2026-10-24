using MudBlazor;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Clubs;

namespace SwissBowling.Client.Features.Clubs.Admin;

public partial class ClubsAdmin
{
    private List<ClubDto> _clubs = [];
    private List<AssociationDto> _associations = [];
    private string _search = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _associations = await Api.GetAsync<List<AssociationDto>>(AssociationRoutes.Base);
        _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base);
    });

    private Task ReloadAsync() => TryAsync(async () => _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base));

    private bool Matches(ClubDto club) =>
        string.IsNullOrWhiteSpace(_search)
        || club.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
        || club.AssociationName.Contains(_search, StringComparison.OrdinalIgnoreCase)
        || (club.City?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);

    private async Task EditAsync(ClubDto? club)
    {
        var parameters = new DialogParameters<ClubDialog> { { d => d.Club, club }, { d => d.Associations, _associations } };
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
