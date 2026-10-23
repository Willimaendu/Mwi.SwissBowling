using System.Security.Claims;
using Mwi.SwissBowling.Api.Features.Results;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.UnitTests;

public sealed class EditingLeaseRulesTests
{
    [Theory]
    [InlineData("issuer", "subject", true, true)]
    [InlineData("other", "subject", true, false)]
    [InlineData("issuer", "other", true, false)]
    [InlineData("issuer", "subject", false, false)]
    public void OwnershipRequiresIssuerSubjectAndToken(string issuer, string subject, bool matchingToken, bool expected)
    {
        var lease = Lease();
        Assert.Equal(expected, EditingLeaseRules.IsOwnedBy(lease, matchingToken ? lease.Token : Guid.NewGuid(), Actor(issuer, subject)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void ExpiryAtOrBeforeNowIsRejected(int seconds)
    {
        var lease = Lease();
        var error = Assert.Throws<RequestRejectedException>(() => EditingLeaseRules.RequireActiveOwner(lease, lease.Token, Actor("issuer", "subject"), lease.ExpiresAt.AddSeconds(-seconds)));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public void ActiveOwnerReceivesTheValidatedLease()
    {
        var lease = Lease();
        Assert.Same(lease, EditingLeaseRules.RequireActiveOwner(lease, lease.Token, Actor("issuer", "subject"), lease.ExpiresAt.AddSeconds(-1)));
    }

    [Fact]
    public void MissingLeaseIsRejected() => Assert.Throws<RequestRejectedException>(() =>
        EditingLeaseRules.RequireActiveOwner(null, Guid.NewGuid(), Actor("issuer", "subject"), DateTimeOffset.UtcNow));

    private static EditingLease Lease() => new() { Token = Guid.NewGuid(), Issuer = "issuer", Subject = "subject", ExpiresAt = DateTimeOffset.Parse("2026-01-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture) };
    private static ClaimsPrincipal Actor(string issuer, string subject) => new(new ClaimsIdentity([new Claim("iss", issuer), new Claim("sub", subject)]));
}
