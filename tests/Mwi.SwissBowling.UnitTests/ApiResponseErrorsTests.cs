using System.Net;
using System.Net.Http.Json;
using Mwi.SwissBowling.Web.Client.Infrastructure;

namespace Mwi.SwissBowling.UnitTests;

public sealed class ApiResponseErrorsTests
{
    [Fact]
    public async Task SuccessfulEmptyResponseDoesNotRequireJson()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);
        await ApiResponseErrors.EnsureSuccessAsync(response);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AuthenticationFailuresDoNotExposeUpstreamDetails(HttpStatusCode status)
    {
        using var response = new HttpResponseMessage(status) { Content = JsonContent.Create(new { title = "private upstream detail" }) };
        var error = await Assert.ThrowsAsync<ApiException>(() => ApiResponseErrors.EnsureSuccessAsync(response));
        Assert.Equal(status, error.StatusCode);
        Assert.DoesNotContain("private upstream detail", error.Message);
    }

    [Fact]
    public async Task InvalidJsonFallsBackToSafeError()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("internal upstream details") };
        var error = await Assert.ThrowsAsync<ApiException>(() => ApiResponseErrors.EnsureSuccessAsync(response));
        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.DoesNotContain("internal upstream details", error.Message);
    }
}
