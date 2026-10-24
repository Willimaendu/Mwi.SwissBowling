using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Championships;

public partial class ChampionshipDetail
{
    [Parameter] public Guid Id { get; set; }

    private ChampionshipDetailDto? _detail;
    private RankingDto? _ranking;
    private PlayerCategory? _category;

    protected override Task OnParametersSetAsync() => LoadAsync(async () =>
    {
        _detail = await Api.GetAsync<ChampionshipDetailDto>(ChampionshipRoutes.ById(Id));
        _ranking = await Api.GetAsync<RankingDto>(ChampionshipRoutes.Ranking(Id, _category));
    });

    private Task LoadRankingAsync(PlayerCategory? category)
    {
        _category = category;
        return TryAsync(async () => _ranking = await Api.GetAsync<RankingDto>(ChampionshipRoutes.Ranking(Id, category)));
    }
}
