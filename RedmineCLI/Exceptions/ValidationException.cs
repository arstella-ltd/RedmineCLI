// This file has been moved to RedmineCLI.Common.Exceptions.ValidationException
// Keeping this for backward compatibility
using RedmineCLI.Common.Exceptions;

namespace RedmineCLI.Exceptions
{
    [Obsolete("Use RedmineCLI.Common.Exceptions.ValidationException instead")]
    public class ValidationException : Common.Exceptions.ValidationException
    {
        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
