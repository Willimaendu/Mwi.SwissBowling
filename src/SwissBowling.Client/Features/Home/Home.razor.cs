using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Home;

public partial class Home
{
    private List<ChampionshipDto> _running = [];

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var all = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.ByYear(today.Year));
        _running = all.Where(c => c.StatusAt(today) == ChampionshipStatus.Running).ToList();
    });
}
