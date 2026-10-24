using Microsoft.AspNetCore.Components;
using SwissBowling.Core.Import;

namespace SwissBowling.Client.Features.Import.Admin;

public partial class ImportStatus
{
    [Parameter] public ImportRunStatus Status { get; set; }
}
