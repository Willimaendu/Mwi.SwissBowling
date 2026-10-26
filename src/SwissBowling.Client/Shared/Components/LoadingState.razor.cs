using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class LoadingState
{
    [Parameter] public bool Loading { get; set; }

    [Parameter] public string? Error { get; set; }

    [Parameter] public bool IsEmpty { get; set; }

    [Parameter] public string EmptyText { get; set; } = "Keine Einträge vorhanden.";

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Platzhalter während des Ladens; ohne Angabe ein Tabellen-Skelett mit <see cref="Columns"/> Spalten.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    [Parameter] public int Columns { get; set; } = 5;
}
