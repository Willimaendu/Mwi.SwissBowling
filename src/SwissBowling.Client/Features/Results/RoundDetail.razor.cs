using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Results;

namespace SwissBowling.Client.Features.Results;

public partial class RoundDetail
{
    [Parameter] public Guid Id { get; set; }

    private RoundDetailDto? _detail;

    protected override Task OnParametersSetAsync() => LoadAsync(async () => _detail = await Api.GetAsync<RoundDetailDto>(ResultRoutes.ForRound(Id)));

    /// <summary>Resultate kommen absteigend sortiert; gleiche Holzzahl teilt den Rang.</summary>
    private int RankOf(ResultDto result) => _detail!.Results.TakeWhile(r => r.Pins > result.Pins).Count() + 1;
}
