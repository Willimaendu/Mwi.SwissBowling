using SwissBowling.Core.Clubs;

namespace SwissBowling.Client.Features.Clubs;

public partial class Clubs
{
    private List<ClubDto> _clubs = [];

    protected override Task OnInitializedAsync() => LoadAsync(async () => _clubs = await Api.GetAsync<List<ClubDto>>(ClubRoutes.Base));
}
