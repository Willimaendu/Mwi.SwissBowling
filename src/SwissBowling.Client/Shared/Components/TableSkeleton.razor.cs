using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class TableSkeleton
{
    [Parameter] public int Rows { get; set; } = 8;

    [Parameter] public int Columns { get; set; } = 5;
}
