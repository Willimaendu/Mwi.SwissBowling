using Microsoft.AspNetCore.Components;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class SetupWorkspace : ComponentBase
{
    [Parameter, EditorRequired] public SetupDto Data { get; set; } = default!;
    [Parameter] public EventCallback Changed { get; set; }

    private bool _busy;
    private string? _error, _message;
    private IEnumerable<MasterDataGrid.Row> PlayerRows => Data.Players.Select(row => new MasterDataGrid.Row($"{row.LastName} {row.FirstName}", $"{row.SfkvNumber ?? "Gast"} · {row.Email}"));
    private IEnumerable<MasterDataGrid.Row> ClubRows => Data.Clubs.Select(row => new MasterDataGrid.Row(row.Name, $"{row.Email} · {row.Phone}"));
    private IEnumerable<MasterDataGrid.Row> SeasonRows => Data.Seasons.Select(row => new MasterDataGrid.Row(row.Year.ToString(), "Saison"));
    private IEnumerable<MasterDataGrid.Row> VenueRows => Data.Venues.Select(row => new MasterDataGrid.Row(row.Name, $"{row.Address} · Bahnen {string.Join(", ", row.Lanes)}"));
    private IEnumerable<MasterDataGrid.Row> AnnualRows => Data.AnnualChampionships.Select(row => new MasterDataGrid.Row(row.Name, Data.Seasons.Single(season => season.Id == row.SeasonId).Year.ToString()));
    private IEnumerable<MasterDataGrid.Row> EventRows => Data.Events.Select(row => new MasterDataGrid.Row(row.Name, $"{row.StartDate:dd.MM.yyyy} – {row.EndDate:dd.MM.yyyy}"));
    private IEnumerable<MasterDataGrid.Row> MemberRows => Data.Members.Select(row =>
    {
        var player = Data.Players.Single(player => player.Id == row.PlayerId);
        var season = Data.Seasons.Single(season => season.Id == row.SeasonId);
        var club = Data.Clubs.SingleOrDefault(club => club.Id == row.ClubId);
        return new MasterDataGrid.Row($"{player.LastName} {player.FirstName}", $"{season.Year} · {club?.Name ?? "Ohne Klub"} · {row.Category}");
    });

    private async Task ExecuteAsync(Func<Task> action)
    {
        _busy = true;
        _error = null;
        _message = null;
        try { await action(); _message = "Gespeichert."; await Changed.InvokeAsync(); }
        catch (ApiException exception) { _error = exception.Message; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        { _error = "Verbindung unterbrochen. Vor erneutem Absenden den gespeicherten Stand prüfen."; }
        finally { _busy = false; }
    }
}
