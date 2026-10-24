using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships;

public partial class ChampionshipCard
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Parameter, EditorRequired] public ChampionshipDto Championship { get; set; } = null!;

    private Color StatusColor => Championship.Status switch
    {
        ChampionshipStatus.Running => Color.Success,
        ChampionshipStatus.Completed => Color.Default,
        _ => Color.Info
    };
}
