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
        _associations = await Api.GetAsync<List<AssociationDto>>(AssociationRoutes.Base);
        var all = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.Base);
        _years = all.Select(c => c.StartDate.Year).Distinct().OrderDescending().ToList();
        _championships = all.Where(c => _year is null || c.StartDate.Year == _year).ToList();
    });

    private Task OnYearChanged(int? year)
    {
        _year = year;
        return LoadAsync(async () => _championships = await Api.GetAsync<List<ChampionshipDto>>(ChampionshipRoutes.ByYear(_year)));
    }
}
