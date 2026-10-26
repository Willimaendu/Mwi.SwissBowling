using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Shared.Components;

public partial class DetailSkeleton
{
    /// <summary>Vier Kennzahl-Kacheln unter dem Titel.</summary>
    [Parameter] public bool WithStats { get; set; } = true;

    /// <summary>Drei Diagrammflächen (Keglerprofil).</summary>
    [Parameter] public bool WithCharts { get; set; }

    /// <summary>Reiterleiste über der Tabelle (Ranglisten nach Disziplin, UV-Detail).</summary>
    [Parameter] public bool WithTabs { get; set; }

    [Parameter] public int TableColumns { get; set; } = 6;
}
