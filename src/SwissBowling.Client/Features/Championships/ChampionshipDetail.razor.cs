using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships;

public partial class ChampionshipDetail
{
    [Parameter] public Guid Id { get; set; }

    private ChampionshipDetailDto? _detail;

    protected override Task OnParametersSetAsync() => LoadAsync(async () => _detail = await Api.GetAsync<ChampionshipDetailDto>(ChampionshipRoutes.ById(Id)));
}
