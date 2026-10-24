using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Associations;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships.Admin;

public partial class ChampionshipDialog
{
    [Parameter] public ChampionshipDto? Championship { get; set; }

    [Parameter] public List<AssociationDto> Associations { get; set; } = [];

    private readonly Model _model = new();

    protected override void OnInitialized()
    {
        if (Championship is { } c)
        {
            (_model.Name, _model.AssociationId, _model.Organizer, _model.Venue, _model.City, _model.Throws) =
                (c.Name, c.AssociationId, c.Organizer, c.Venue, c.City, c.Throws);
            _model.Start = c.StartDate.ToDateTime(TimeOnly.MinValue);
            _model.End = c.EndDate.ToDateTime(TimeOnly.MinValue);
        }
        else
        {
            _model.AssociationId = Associations.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SaveChampionshipRequest(_model.Name, _model.AssociationId, _model.Organizer, _model.Venue, _model.City,
                DateOnly.FromDateTime(_model.Start!.Value), DateOnly.FromDateTime(_model.End!.Value), _model.Throws);
            return Championship is { } c
                ? Api.PutAsync<ChampionshipDto>(ChampionshipRoutes.ById(c.Id), request)
                : Api.PostAsync<ChampionshipDto>(ChampionshipRoutes.Base, request);
        },
        "Meisterschaft gespeichert.");

    private sealed class Model
    {
        public string Name { get; set; } = string.Empty;

        public Guid AssociationId { get; set; }

        public string? Organizer { get; set; }

        public string? Venue { get; set; }

        public string? City { get; set; }

        public DateTime? Start { get; set; } = DateTime.Today;

        public DateTime? End { get; set; } = DateTime.Today.AddDays(13);

        public int Throws { get; set; } = 100;
    }
}
