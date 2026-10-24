using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Seasons;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class ChampionshipDialog
{
    [Parameter] public ChampionshipDto? Championship { get; set; }

    [Parameter] public List<SeasonDto> Seasons { get; set; } = [];

    private readonly Model _model = new();

    protected override void OnInitialized()
    {
        if (Championship is { } c)
        {
            (_model.Name, _model.Description, _model.SeasonId, _model.NumberOfRounds, _model.DiscardedRounds, _model.ThrowsPerRound) =
                (c.Name, c.Description, c.SeasonId, c.NumberOfRounds, c.DiscardedRounds, c.ThrowsPerRound);
        }
        else
        {
            _model.SeasonId = Seasons.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveChampionshipRequest(_model.Name, _model.Description, _model.SeasonId, _model.NumberOfRounds, _model.DiscardedRounds, _model.ThrowsPerRound);
            return Championship is { } c
                ? Api.PutAsync<ChampionshipDto>(ChampionshipRoutes.ById(c.Id), request)
                : Api.PostAsync<ChampionshipDto>(ChampionshipRoutes.Base, request);
        },
        "Meisterschaft gespeichert.");

    private sealed class Model
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid SeasonId { get; set; }

        public int NumberOfRounds { get; set; } = 6;

        public int DiscardedRounds { get; set; } = 1;

        public int ThrowsPerRound { get; set; } = 120;
    }
}
