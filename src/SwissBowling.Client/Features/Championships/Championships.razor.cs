using SwissBowling.Core.Associations;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships;

public partial class Championships
{
    private List<AssociationDto> _associations = [];
    private List<ChampionshipDto> _championships = [];
    private List<int> _years = [];
    private int? _year = DateTime.Today.Year;
    private Guid? _associationId;

    private IEnumerable<ChampionshipDto> Filtered => _championships.Where(c => _associationId is null || c.AssociationId == _associationId);

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _associations = await FetchAsync<List<AssociationDto>>("associations", AssociationRoutes.Base);
        _years = await FetchAsync<List<int>>("years", ChampionshipRoutes.Years);
        if (_year is { } year && !_years.Contains(year))
        {
            // Noch nichts im laufenden Jahr: das jüngste Jahr mit Meisterschaften zeigen.
            _year = _years.Count > 0 ? _years[0] : null;
        }

        _championships = await FetchAsync<List<ChampionshipDto>>("championships", ChampionshipRoutes.ByYear(_year));
    });

    private Task OnYearChanged(int? year)
    {
        _year = year;
        return LoadAsync(async () => _championships = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.ByYear(_year)));
    }
}
