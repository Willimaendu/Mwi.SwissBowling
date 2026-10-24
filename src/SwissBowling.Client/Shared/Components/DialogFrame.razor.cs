using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class DialogFrame
{
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }

    [Parameter] public string? Error { get; set; }

    [Parameter] public bool Busy { get; set; }

    [Parameter] public bool CanSave { get; set; } = true;

    [Parameter] public string SaveText { get; set; } = "Speichern";

    [Parameter] public EventCallback OnSave { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }
}
