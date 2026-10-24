using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships;

public partial class RankingTabs
{
    [Parameter, EditorRequired] public IReadOnlyList<RankingEntryDto> Ranking { get; set; } = [];
}
