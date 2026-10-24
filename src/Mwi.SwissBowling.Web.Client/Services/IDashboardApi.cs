namespace Mwi.SwissBowling.Web.Client.Services;

public interface IDashboardApi
{
    Task<T> ReadAsync<T>(string resource, CancellationToken cancellationToken = default);
    Task<T> CreateAsync<T>(string resource, object input, CancellationToken cancellationToken = default);
    Task CreateAsync(string resource, object input, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync<T>(string resource, object input, CancellationToken cancellationToken = default);
    Task UpdateAsync(string resource, object input, CancellationToken cancellationToken = default);
}

public sealed class DashboardApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
