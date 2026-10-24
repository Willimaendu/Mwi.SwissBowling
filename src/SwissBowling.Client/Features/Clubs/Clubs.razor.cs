using MudBlazor;
using SwissBowling.Core.Clubs;

namespace SwissBowling.Client.Features.Clubs;

public partial class Clubs
{
    private List<ClubDto> _clubs = [];
    private string _search = string.Empty;

    private readonly TableGroupDefinition<ClubDto> _groupByAssociation = new()
    {
        Indentation = false,
        Expandable = true,
        IsInitiallyExpanded = true,
        Selector = c => c.AssociationName
    };

    protected override Task OnInitializedAsync() => LoadAsync(async () => _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base));

    private bool Matches(ClubDto club) =>
        string.IsNullOrWhiteSpace(_search)
        || club.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
        || (club.HomeVenue?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);
}
