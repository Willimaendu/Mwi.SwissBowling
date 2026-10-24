using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class LoadingState
{
    [Parameter] public bool Loading { get; set; }

    [Parameter] public string? Error { get; set; }

    [Parameter] public bool IsEmpty { get; set; }

    [Parameter] public string EmptyText { get; set; } = "Keine Einträge vorhanden.";

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
