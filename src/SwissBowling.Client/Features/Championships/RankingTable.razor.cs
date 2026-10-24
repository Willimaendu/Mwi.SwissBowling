using Microsoft.AspNetCore.Components;
using MudBlazor;
using SwissBowling.Core.Championships;

namespace SwissBowling.Client.Features.Championships;

public partial class RankingTable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Parameter, EditorRequired] public IReadOnlyList<RankingEntryDto> Entries { get; set; } = [];

    private string _search = string.Empty;

    private bool Matches(RankingEntryDto entry) =>
        string.IsNullOrWhiteSpace(_search)
        || entry.PlayerName.Contains(_search, StringComparison.OrdinalIgnoreCase)
        || (entry.ClubName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false);

    private void OnRowClick(TableRowClickEventArgs<RankingEntryDto> args)
    {
        if (args.Item is { } entry)
        {
            Navigation.NavigateTo($"/kegler/{entry.PlayerId}");
        }
    }
}
