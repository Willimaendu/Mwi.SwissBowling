using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players;

public partial class PlayerProfile
{
    [Parameter] public Guid Id { get; set; }

    private PlayerDetailDto? _detail;

    protected override Task OnParametersSetAsync() => LoadAsync(async () => _detail = await FetchAsync<PlayerDetailDto>($"player-{Id}", PlayerRoutes.ById(Id)));
}
