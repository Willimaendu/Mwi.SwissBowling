using Microsoft.AspNetCore.Components;

namespace SwissBowling.Client.Features.Import.Admin;

public partial class ImportStatus
{
    [Parameter] public bool IsImported { get; set; }
}
