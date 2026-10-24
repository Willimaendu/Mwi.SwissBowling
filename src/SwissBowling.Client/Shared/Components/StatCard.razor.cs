using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class StatCard
{
    [Parameter, EditorRequired] public string Label { get; set; } = string.Empty;

    [Parameter, EditorRequired] public string Value { get; set; } = string.Empty;
}
