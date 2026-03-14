namespace LibSyncthing;

public class SyncthingApiException : Exception
{
    public int StatusCode { get; }

    public SyncthingApiException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public SyncthingApiException(string message, int statusCode, Exception innerException)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
