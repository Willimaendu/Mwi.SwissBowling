using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Mwi.SwissBowling.Web.Features.LiveUpdates;

public sealed class PublicUpdatesHub : Hub;

[Authorize(Policy = "BffSession")]
public sealed class AdministrationUpdatesHub : Hub;
