using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;

namespace SwissBowling.Core;

/// <summary>Deutschsprachige Anzeigetexte für Enums.</summary>
public static class Labels
{
    public static string Label(this PlayerCategory category) => $"Kat. {category}";

    public static string Label(this Discipline discipline) => discipline switch
    {
        Discipline.Championship => "Meisterschaft",
        Discipline.Women => "MS Damen",
        _ => discipline.ToString()
    };

    public static string Label(this ChampionshipStatus status) => status switch
    {
        ChampionshipStatus.Planned => "Geplant",
        ChampionshipStatus.Running => "Laufend",
        ChampionshipStatus.Completed => "Abgeschlossen",
        _ => status.ToString()
    };
}
