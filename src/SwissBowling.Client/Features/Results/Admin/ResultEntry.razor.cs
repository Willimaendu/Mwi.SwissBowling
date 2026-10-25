using Microsoft.AspNetCore.Components;
using SwissBowling.Core;
using SwissBowling.Core.Championships;
using SwissBowling.Core.Players;
using SwissBowling.Core.Results;

namespace SwissBowling.Client.Features.Results.Admin;

public partial class ResultEntry
{
    [Parameter] public Guid Id { get; set; }

    private ChampionshipDto? _championship;
    private List<PlayerDto> _players = [];
    private List<ResultDto> _saved = [];
    private List<Row> _rows = [];
    private Discipline _discipline = Discipline.Championship;
    private string _search = string.Empty;
    private bool _onlyWithResult;

    private int DirtyCount => _rows.Count(r => r.IsDirty);

    protected override Task OnParametersSetAsync() => LoadAsync(async () =>
    {
        _championship = (await Api.GetAsync<ChampionshipDetailDto>(ChampionshipRoutes.ById(Id))).Championship;
        _saved = await Api.GetAsync<List<ResultDto>>(ResultRoutes.ForChampionship(Id));
        // Erfasst werden Kegler des veranstaltenden Unterverbands; Gäste anderer UVs kommen über den Import.
        _players = (await Api.GetAsync<PagedResult<PlayerDto>>(PlayerRoutes.Search(new PlayerFilter(AssociationId: _championship.AssociationId), 1, PlayerRoutes.MaxPageSize))).Items.ToList();
        BuildRows();
    });

    private void BuildRows()
    {
        var byPlayer = _saved.Where(r => r.Discipline == _discipline).ToDictionary(r => r.PlayerId);
        _rows = _players.Select(p => new Row(p, byPlayer.GetValueOrDefault(p.Id))).ToList();
    }

    private Task SwitchDisciplineAsync(Discipline discipline)
    {
        _discipline = discipline;
        BuildRows();
        return Task.CompletedTask;
    }

    private bool Matches(Row row) =>
        (!_onlyWithResult || row.Pins is not null)
        && (string.IsNullOrWhiteSpace(_search)
            || row.Player.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)
            || (row.Player.ClubName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));

    private async Task SaveAsync()
    {
        var changed = _rows.Where(r => r.IsDirty).ToList();
        var inputs = changed.Where(r => r.Pins is not null)
            .Select(r => new ResultInput(r.Player.Id, _discipline, r.Pins!.Value, r.Nines, r.Award, r.Date is { } d ? DateOnly.FromDateTime(d) : null))
            .ToList();
        var removed = changed.Where(r => r.Pins is null && r.Saved is not null).ToList();

        await TryAsync(async () =>
        {
            foreach (var row in removed)
            {
                await Api.DeleteAsync(ResultRoutes.ById(row.Saved!.Id));
            }

            _saved = inputs.Count > 0
                ? await Api.PutAsync<List<ResultDto>>(ResultRoutes.ForChampionship(Id), new SaveResultsRequest(inputs))
                : await Api.GetAsync<List<ResultDto>>(ResultRoutes.ForChampionship(Id));

            var byPlayer = _saved.Where(r => r.Discipline == _discipline).ToDictionary(r => r.PlayerId);
            foreach (var row in changed)
            {
                row.MarkSaved(byPlayer.GetValueOrDefault(row.Player.Id));
            }
        }, $"{changed.Count} Resultate gespeichert.");
    }

    private sealed class Row
    {
        public Row(PlayerDto player, ResultDto? saved)
        {
            Player = player;
            MarkSaved(saved);
        }

        public PlayerDto Player { get; }

        public ResultDto? Saved { get; private set; }

        public int? Pins { get; set; }

        public int Nines { get; set; }

        public bool Award { get; set; }

        public DateTime? Date { get; set; }

        public bool IsDirty =>
            Pins != Saved?.Pins
            || (Pins is not null && (Nines != Saved?.Nines || Award != Saved?.Award || DateOnlyOrNull(Date) != Saved?.Date));

        public void MarkSaved(ResultDto? saved)
        {
            Saved = saved;
            Pins = saved?.Pins;
            Nines = saved?.Nines ?? 0;
            Award = saved?.Award ?? false;
            Date = saved is null ? DateTime.Today : saved.Date?.ToDateTime(TimeOnly.MinValue);
        }

        private static DateOnly? DateOnlyOrNull(DateTime? value) => value is { } d ? DateOnly.FromDateTime(d) : null;
    }
}
