using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Clubs.Admin;

public partial class ClubDialog
{
    [Parameter] public ClubDto? Club { get; set; }

    [Parameter] public List<AssociationDto> Associations { get; set; } = [];

    private readonly Model _model = new();

    protected override void OnInitialized()
    {
        if (Club is { } c)
        {
            (_model.Name, _model.City, _model.Category, _model.AssociationId, _model.HomeVenue, _model.Email, _model.Website) =
                (c.Name, c.City, c.Category, c.AssociationId, c.HomeVenue, c.Email, c.Website);
        }
        else
        {
            _model.AssociationId = Associations.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveClubRequest(_model.Name, _model.City, _model.Category, _model.HomeVenue, _model.Email, _model.Website, _model.AssociationId);
            return Club is { } c
                ? Api.PutAsync<ClubDto>(ClubRoutes.ById(c.Id), request)
                : Api.PostAsync<ClubDto>(ClubRoutes.Base, request);
        },
        "Klub gespeichert.");

    private sealed class Model
    {
        public string Name { get; set; } = string.Empty;

        public string? City { get; set; }

        public PlayerCategory? Category { get; set; }

        public Guid AssociationId { get; set; }

        public string? HomeVenue { get; set; }

        public string? Email { get; set; }

        public string? Website { get; set; }
    }
}
