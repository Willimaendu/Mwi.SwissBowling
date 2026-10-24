using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class RankBadge
{
    [Parameter, EditorRequired] public int Rank { get; set; }
}
