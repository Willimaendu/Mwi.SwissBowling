using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships;

public partial class ChampionshipDetail
{
    [Parameter] public Guid Id { get; set; }

    /// <summary>Kegler, von dessen Profil aus die Seite geöffnet wurde – seine Zeile wird hervorgehoben.</summary>
    [SupplyParameterFromQuery(Name = "kegler")]
    public Guid? HighlightPlayerId { get; set; }

    private ChampionshipDetailDto? _detail;

    protected override Task OnParametersSetAsync() => LoadAsync(async () => _detail = await FetchAsync<ChampionshipDetailDto>($"championship-{Id}", ChampionshipRoutes.ById(Id)));
}
