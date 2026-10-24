using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players;

public partial class PlayerProfile
{
    [Parameter] public Guid Id { get; set; }

    private PlayerDetailDto? _detail;

    private readonly TableGroupDefinition<PlayerResultDto> _groupByChampionship = new()
    {
        Indentation = false,
        Expandable = true,
        IsInitiallyExpanded = true,
        Selector = r => r.ChampionshipName
    };

    protected override Task OnParametersSetAsync() => LoadAsync(async () => _detail = await Api.GetAsync<PlayerDetailDto>(PlayerRoutes.ById(Id)));
}
