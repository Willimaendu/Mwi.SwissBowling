using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;

namespace SwissBowling.Client.Features.Championships;

public partial class RankingTabs
{
    [Parameter, EditorRequired] public IReadOnlyList<RankingEntryDto> Ranking { get; set; } = [];

    /// <summary>Optional hervorzuhebender Kegler; seine Disziplin und Kategorie werden vorausgewählt.</summary>
    [Parameter] public Guid? HighlightPlayerId { get; set; }

    private RankingEntryDto? _highlight;
    private int _disciplineIndex;
    private int _categoryIndex;

    private IEnumerable<IGrouping<Discipline, RankingEntryDto>> Disciplines => Ranking.GroupBy(r => r.Discipline).OrderBy(g => g.Key);

    private static IEnumerable<IGrouping<PlayerCategory, RankingEntryDto>> Categories(IEnumerable<RankingEntryDto> discipline) =>
        discipline.GroupBy(r => r.Category).OrderBy(g => g.Key);

    protected override void OnParametersSet()
    {
        _highlight = HighlightPlayerId is { } id ? Ranking.FirstOrDefault(r => r.PlayerId == id) : null;
        var disciplines = Disciplines.ToList();
        _disciplineIndex = _highlight is null ? 0 : Math.Max(0, disciplines.FindIndex(g => g.Key == _highlight.Discipline));
        _categoryIndex = _highlight is null ? 0 : Math.Max(0, Categories(disciplines[_disciplineIndex]).ToList().FindIndex(g => g.Key == _highlight.Category));
    }
}
