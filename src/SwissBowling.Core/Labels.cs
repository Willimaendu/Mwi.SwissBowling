using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;

namespace SwissBowling.Core;

/// <summary>Deutschsprachige Anzeigetexte für Enums.</summary>
public static class Labels
{
    public static string Label(this PlayerCategory category) => category.ToString();

    public static string Label(this ChampionshipStatus status) => status switch
    {
        ChampionshipStatus.Planned => "Geplant",
        ChampionshipStatus.Running => "Laufend",
        ChampionshipStatus.Completed => "Abgeschlossen",
        _ => status.ToString()
    };
}
