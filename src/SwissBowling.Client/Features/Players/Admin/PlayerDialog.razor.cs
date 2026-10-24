using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Clubs;
using SwissBowling.Core.Players;

namespace SwissBowling.Client.Features.Players.Admin;

public partial class PlayerDialog
{
    [Parameter] public PlayerDto? Player { get; set; }

    [Parameter] public List<ClubDto> Clubs { get; set; } = [];

    private readonly Model _model = new();

    protected override void OnInitialized()
    {
        if (Player is { } p)
        {
            (_model.Name, _model.City, _model.LicenseNumber, _model.Category, _model.ClubId, _model.IsActive) =
                (p.Name, p.City, p.LicenseNumber, p.Category, p.ClubId, p.IsActive);
        }
        else
        {
            _model.ClubId = Clubs.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SavePlayerRequest(_model.Name, _model.City, _model.LicenseNumber, _model.Category, _model.ClubId, _model.IsActive);
            return Player is { } p
                ? Api.PutAsync<PlayerDto>(PlayerRoutes.ById(p.Id), request)
                : Api.PostAsync<PlayerDto>(PlayerRoutes.Base, request);
        },
        "Kegler gespeichert.");

    private sealed class Model
    {
        public string Name { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? LicenseNumber { get; set; }

        public PlayerCategory Category { get; set; } = PlayerCategory.A;

        public Guid ClubId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
