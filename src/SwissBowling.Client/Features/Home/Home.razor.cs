using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Home;

public partial class Home
{
    private List<ChampionshipDto> _running = [];

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        var all = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.Base);
        _running = all.Where(c => c.Status == ChampionshipStatus.Running).ToList();
    });
}
