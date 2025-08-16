// This file has been moved to RedmineCLI.Common.Exceptions.RedmineApiException
// Keeping this for backward compatibility
using RedmineCLI.Common.Exceptions;

namespace RedmineCLI.Exceptions
{
    [Obsolete("Use RedmineCLI.Common.Exceptions.RedmineApiException instead")]
    public class RedmineApiException : Common.Exceptions.RedmineApiException
    {
        public RedmineApiException(int statusCode, string message, string? apiError = null)
            : base(statusCode, message, apiError)
        {
        }
    }
}
