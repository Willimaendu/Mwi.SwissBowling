using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players;

public partial class Players
{
    [SupplyParameterFromQuery(Name = "club")]
    public Guid? ClubId { get; set; }

    private List<PlayerDto> _players = [];
    private string _search = string.Empty;
    private PlayerCategory? _category;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
        _players = await Api.GetAsync<List<PlayerDto>>(PlayerRoutes.All(includeInactive: false)));

    private bool Matches(PlayerDto player) =>
        (ClubId is null || player.ClubId == ClubId)
        && (_category is null || player.Category == _category)
        && (string.IsNullOrWhiteSpace(_search)
            || player.FullName.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || player.ClubName.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || (player.LicenseNumber?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));
}
