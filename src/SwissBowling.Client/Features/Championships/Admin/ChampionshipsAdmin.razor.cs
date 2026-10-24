using MudBlazor;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Seasons;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class ChampionshipsAdmin
{
    private List<SeasonDto> _seasons = [];
    private List<ChampionshipDto> _championships = [];

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _seasons = await Api.GetAsync<List<SeasonDto>>(SeasonRoutes.Base);
        _championships = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.Base);
    });

    private async Task CreateAsync()
    {
        var parameters = new DialogParameters<ChampionshipDialog> { { d => d.Seasons, _seasons } };
        if (await ShowDialogAsync<ChampionshipDialog, ChampionshipDto>("Neue Meisterschaft", parameters) is { } created)
        {
            Navigation.NavigateTo($"/admin/meisterschaften/{created.Id}");
        }
    }
}
