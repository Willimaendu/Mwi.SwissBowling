using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class CardSkeleton
{
    [Parameter] public int Count { get; set; } = 6;

    [Parameter] public int Sm { get; set; } = 12;

    [Parameter] public int Md { get; set; } = 6;

    [Parameter] public int Lg { get; set; } = 6;

    /// <summary>Dritte Textzeile (Meisterschaftskarten zeigen Lokal und Ort).</summary>
    [Parameter] public bool ThreeLines { get; set; }

    /// <summary>Kennzahl rechts (Meisterschaftskarten zeigen die Resultatanzahl).</summary>
    [Parameter] public bool WithFigure { get; set; }
}
