using SwissBowling.Core.Championships;
using SwissBowling.Core.Seasons;

namespace SwissBowling.Client.Features.Championships;

public partial class Championships
{
    private List<SeasonDto> _seasons = [];
    private List<ChampionshipDto> _championships = [];
    private Guid? _seasonId;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _seasons = await Api.GetAsync<List<SeasonDto>>(SeasonRoutes.Base);
        _championships = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.BySeason(_seasonId));
    });

    private Task OnSeasonChanged(Guid? seasonId)
    {
        _seasonId = seasonId;
        return LoadAsync(async () => _championships = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.BySeason(_seasonId)));
    }
}
