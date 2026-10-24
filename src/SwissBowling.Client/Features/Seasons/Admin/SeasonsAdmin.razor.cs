using MudBlazor;
using SwissBowling.Core.Seasons;

namespace SwissBowling.Client.Features.Seasons.Admin;

public partial class SeasonsAdmin
{
    private List<SeasonDto> _seasons = [];

    protected override Task OnInitializedAsync() => ReloadAsync();

    private Task ReloadAsync() => LoadAsync(async () => _seasons = await Api.GetAsync<List<SeasonDto>>(SeasonRoutes.Base));

    private async Task EditAsync(SeasonDto? season)
    {
        var parameters = new DialogParameters<SeasonDialog> { { d => d.Season, season } };
        if (await ShowDialogAsync<SeasonDialog, SeasonDto>(season is null ? "Neue Saison" : "Saison bearbeiten", parameters) is not null)
        {
            await ReloadAsync();
        }
    }

    private async Task DeleteAsync(SeasonDto season)
    {
        if (await ConfirmAsync("Saison löschen", $"Soll die Saison «{season.Name}» gelöscht werden?")
            && await TryAsync(() => Api.DeleteAsync(SeasonRoutes.ById(season.Id)), "Saison gelöscht."))
        {
            await ReloadAsync();
        }
    }
}
