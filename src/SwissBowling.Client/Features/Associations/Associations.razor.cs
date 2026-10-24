using SwissBowling.Core.Associations;

namespace SwissBowling.Client.Features.Associations;

public partial class Associations
{
    private List<AssociationDto> _items = [];

    protected override Task OnInitializedAsync() => LoadAsync(async () => _items = await FetchAsync<List<AssociationDto>>("associations", AssociationRoutes.Base));
}
