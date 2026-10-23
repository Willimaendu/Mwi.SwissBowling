namespace Mwi.SwissBowling.Core.Http;

public sealed class RequestRejectedException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
