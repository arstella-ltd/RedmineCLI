namespace RedmineCLI.Common.Exceptions;

/// <summary>
/// Exception thrown when Redmine API operations fail
/// </summary>
public class RedmineApiException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the API
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// The error message from the API response, if available
    /// </summary>
    public string? ApiError { get; }

    public RedmineApiException(int statusCode, string message, string? apiError = null)
        : base(message)
    {
        StatusCode = statusCode;
        ApiError = apiError;
    }
}
