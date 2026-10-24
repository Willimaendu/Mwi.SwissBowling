using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SwissBowling.Core.Auth;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;

namespace SwissBowling.Client.Features.Results.Admin;

public partial class ResultEntry
{
    [Parameter] public Guid Id { get; set; }

    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    private RoundDetailDto? _detail;
    private List<Row> _rows = [];
    private string _search = string.Empty;
    private bool _onlyWithResult;
    private bool _isAdmin;

    private bool CanEdit => _detail is { } d && (!d.Round.IsCompleted || _isAdmin);

    private int DirtyCount => _rows.Count(r => r.IsDirty);

    protected override async Task OnParametersSetAsync()
    {
        if (AuthState is not null)
        {
            _isAdmin = (await AuthState).User.IsInRole(Roles.Admin);
        }

        await LoadAsync(async () =>
        {
            _detail = await Api.GetAsync<RoundDetailDto>(ResultRoutes.ForRound(Id));
            var players = await Api.GetAsync<List<PlayerDto>>(PlayerRoutes.All(includeInactive: false));
            var saved = _detail.Results.ToDictionary(r => r.PlayerId, r => r.Pins);
            _rows = players.Select(p => new Row(p, saved.TryGetValue(p.Id, out var pins) ? pins : null)).ToList();
        });
    }

    private bool Matches(Row row) =>
        (!_onlyWithResult || row.Pins is not null || row.SavedPins is not null)
        && (string.IsNullOrWhiteSpace(_search)
            || row.Player.FullName.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || row.Player.ClubShortName.Contains(_search, StringComparison.OrdinalIgnoreCase));

    private async Task SaveAsync()
    {
        var changed = _rows.Where(r => r.IsDirty).ToList();
        var inputs = changed.Where(r => r.Pins is not null).Select(r => new ResultInput(r.Player.Id, r.Pins!.Value)).ToList();
        var removed = changed.Where(r => r.Pins is null).ToList();

        var ok = await TryAsync(async () =>
        {
            foreach (var row in removed)
            {
                var result = _detail!.Results.First(x => x.PlayerId == row.Player.Id);
                await Api.DeleteAsync(ResultRoutes.ById(result.Id));
            }

            if (inputs.Count > 0)
            {
                _detail = await Api.PutAsync<RoundDetailDto>(ResultRoutes.ForRound(Id), new SaveResultsRequest(inputs));
            }
            else
            {
                _detail = await Api.GetAsync<RoundDetailDto>(ResultRoutes.ForRound(Id));
            }

            foreach (var row in changed)
            {
                row.SavedPins = row.Pins;
            }
        }, $"{changed.Count} Resultate gespeichert.");

        if (!ok)
        {
            await OnParametersSetAsync();
        }
    }

    private async Task ToggleCompletedAsync()
    {
        var completed = !_detail!.Round.IsCompleted;
        if (completed && !await ConfirmAsync("Runde abschliessen", "Danach sind Änderungen nur noch Administratoren erlaubt.", "Abschliessen"))
        {
            return;
        }

        await TryAsync(
            async () => _detail = _detail with { Round = await Api.PutAsync<RoundDto>(ChampionshipRoutes.RoundCompletion(Id), new SetRoundCompletedRequest(completed)) },
            completed ? "Runde abgeschlossen." : "Runde wieder geöffnet.");
    }

    private sealed class Row(PlayerDto player, int? savedPins)
    {
        public PlayerDto Player { get; } = player;

        public int? Pins { get; set; } = savedPins;

        public int? SavedPins { get; set; } = savedPins;

        public bool IsDirty => Pins != SavedPins;
    }
}
