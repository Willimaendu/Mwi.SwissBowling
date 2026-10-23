using System.Security.Claims;
using Mwi.SwissBowling.Api.Features.Identity;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.Api.Features.Results;

public static class EditingLeaseRules
{
    public static bool IsOwnedBy(EditingLease lease, Guid token, ClaimsPrincipal user) =>
        lease.Token == token && lease.Subject == user.Subject() && lease.Issuer == user.Issuer();

    public static EditingLease RequireActiveOwner(EditingLease? lease, Guid token, ClaimsPrincipal user, DateTimeOffset now)
    {
        if (lease is null || !IsOwnedBy(lease, token, user) || lease.ExpiresAt <= now)
            throw new RequestRejectedException(409, "Die Bearbeitungssperre ist abgelaufen oder gehört nicht zu Ihrer Sitzung.");
        return lease;
    }
}
