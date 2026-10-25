using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players;

public partial class Players
{
    [SupplyParameterFromQuery(Name = "club")]
    public Guid? ClubId { get; set; }

    [SupplyParameterFromQuery(Name = "association")]
    public Guid? AssociationId { get; set; }

    private List<PlayerDto> _players = [];
    private List<ClubDto> _clubs = [];
    private ClubDto? _club;
    private string _search = string.Empty;
    private PlayerCategory? _category;

    protected override Task OnInitializedAsync() => LoadAsync(async () =>
    {
        _players = await FetchAsync<List<PlayerDto>>("players", PlayerRoutes.All(includeInactive: false));
        _clubs = await FetchAsync<List<ClubDto>>("clubs", ClubRoutes.Base);
        _club = _clubs.FirstOrDefault(c => c.Id == ClubId);
    });

    private Task<IEnumerable<ClubDto>> SearchClubsAsync(string? text, CancellationToken ct) =>
        Task.FromResult(_clubs.Where(c => string.IsNullOrWhiteSpace(text)
            || c.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
            || (c.City?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)));

    private bool Matches(PlayerDto player) =>
        (_club is null || player.ClubId == _club.Id)
        && (AssociationId is null || player.AssociationId == AssociationId)
        && (_category is null || player.Category == _category)
        && (string.IsNullOrWhiteSpace(_search)
            || player.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || (player.LicenseNumber?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));
}
