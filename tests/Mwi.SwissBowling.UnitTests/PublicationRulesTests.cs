using Mwi.SwissBowling.Api.Features.Publication;
using Mwi.SwissBowling.Contracts;
using Mwi.SwissBowling.Core.Http;

namespace Mwi.SwissBowling.UnitTests;

public sealed class PublicationRulesTests
{
    [Theory]
    [InlineData(EventState.Draft)]
    [InlineData(EventState.Correcting)]
    public void DraftAndCorrectionMayPublish(EventState state) => PublicationRules.RequirePublishable(state, 1, false);
    [Theory]
    [InlineData(EventState.Published, 1, false)]
    [InlineData(EventState.Draft, 0, false)]
    [InlineData(EventState.Draft, 1, true)]
    public void RejectsAlreadyPublishedEmptyOrActivelyEditedEvents(EventState state, int results, bool leases) => Assert.Throws<RequestRejectedException>(() => PublicationRules.RequirePublishable(state, results, leases));
    [Fact]
    public void RequesterCannotApproveOwnCorrection() => Assert.Throws<RequestRejectedException>(() => PublicationRules.RequireDifferentApprover("issuer", "a", "issuer", "a"));
    [Fact]
    public void DifferentActorMayApprove() => PublicationRules.RequireDifferentApprover("issuer", "a", "issuer", "b");
}
