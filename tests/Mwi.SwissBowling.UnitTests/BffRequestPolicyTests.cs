using Mwi.SwissBowling.Web.Authentication;

namespace Mwi.SwissBowling.UnitTests;

public sealed class BffRequestPolicyTests
{
    [Theory]
    [InlineData("/administration", true)]
    [InlineData("/", true)]
    [InlineData("//external.test", false)]
    [InlineData("/\\external.test", false)]
    [InlineData("https://external.test", false)]
    [InlineData("administration", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ReturnUrlMustBeLocal(string? value, bool expected) =>
        Assert.Equal(expected, BffRequestPolicy.IsLocalReturnUrl(value));

    [Theory]
    [InlineData("identity/me", true)]
    [InlineData("setup", true)]
    [InlineData("events/123/results", true)]
    [InlineData("public/events", false)]
    [InlineData("setup/../identity", false)]
    [InlineData("setup/./players", false)]
    [InlineData("setup/\\players", false)]
    [InlineData("https://external.test", false)]
    [InlineData("/setup", false)]
    [InlineData("", false)]
    public void RelayRequiresAnAllowedFeatureRootWithoutTraversal(string path, bool expected) =>
        Assert.Equal(expected, BffRequestPolicy.IsAllowedApiPath(path));
}
