using Microsoft.AspNetCore.Components;

namespace Mwi.SwissBowling.Web.Client.Features.Administration;

public partial class MasterDataGrid : ComponentBase
{
    [Parameter, EditorRequired] public string Title { get; set; } = "";
    [Parameter, EditorRequired] public IEnumerable<Row> Items { get; set; } = [];

    private string _search = "";
    private bool Matches(Row row) => $"{row.Name} {row.Details}".Contains(_search, StringComparison.CurrentCultureIgnoreCase);
    public sealed record Row(string Name, string Details);
}
