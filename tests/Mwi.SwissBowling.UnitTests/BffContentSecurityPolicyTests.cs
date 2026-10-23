using Mwi.SwissBowling.Web.Authentication;

namespace Mwi.SwissBowling.UnitTests;

public sealed class BffContentSecurityPolicyTests
{
    [Theory]
    [InlineData("http://localhost:8080/realms/swissbowling", "http://localhost:8080")]
    [InlineData("https://identity.example.test/realms/swissbowling", "https://identity.example.test")]
    [InlineData("https://identity.example.test:8443/realms/swissbowling", "https://identity.example.test:8443")]
    public void FormActionAllowsOnlySelfAndTheConfiguredIdentityOrigin(string authority, string origin)
    {
        var policy = BffContentSecurityPolicy.Create(authority);

        Assert.Equal($"default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self' {origin}", policy);
        Assert.DoesNotContain("/realms/", policy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/realms/swissbowling")]
    [InlineData("file:///realms/swissbowling")]
    [InlineData("ftp://identity.example.test/realm")]
    [InlineData("https://user:password@identity.example.test/realm")]
    [InlineData("https://identity.example.test;form-action *")]
    public void InvalidAuthoritiesCannotBecomeCspSources(string? authority) =>
        Assert.Throws<InvalidOperationException>(() => BffContentSecurityPolicy.Create(authority));
}
