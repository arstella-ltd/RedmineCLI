namespace RedmineCLI.Exceptions;

public class RedmineApiException : Exception
{
    public int StatusCode { get; }
    public string? ApiError { get; }

    public RedmineApiException(int statusCode, string message, string? apiError = null)
        : base(message)
    {
        StatusCode = statusCode;
        ApiError = apiError;
    }
}
