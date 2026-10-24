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
            (_model.FirstName, _model.LastName, _model.BirthYear, _model.Category, _model.LicenseNumber, _model.ClubId, _model.IsActive) =
                (p.FirstName, p.LastName, p.BirthYear, p.Category, p.LicenseNumber, p.ClubId, p.IsActive);
        }
        else
        {
            _model.ClubId = Clubs.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    private Task SaveAsync() => SubmitAsync(
        () =>
        {
            var request = new SavePlayerRequest(_model.FirstName, _model.LastName, _model.BirthYear, _model.Category, _model.LicenseNumber, _model.ClubId, _model.IsActive);
            return Player is { } p
                ? Api.PutAsync<PlayerDto>(PlayerRoutes.ById(p.Id), request)
                : Api.PostAsync<PlayerDto>(PlayerRoutes.Base, request);
        },
        "Kegler gespeichert.");

    private sealed class Model
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int BirthYear { get; set; } = 1990;

        public PlayerCategory Category { get; set; } = PlayerCategory.Herren;

        public string? LicenseNumber { get; set; }

        public Guid ClubId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
