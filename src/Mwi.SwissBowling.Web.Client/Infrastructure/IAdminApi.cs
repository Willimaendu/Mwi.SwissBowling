namespace Mwi.SwissBowling.Web.Client.Infrastructure;

public interface IAdminApi
{
    Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default);
    Task PostAsync(string path, object body, CancellationToken cancellationToken = default);
    Task PutAsync(string path, object body, CancellationToken cancellationToken = default);
    Task<T> PostAsync<T>(string path, object body, CancellationToken cancellationToken = default);
    Task<T> PutAsync<T>(string path, object body, CancellationToken cancellationToken = default);
}
