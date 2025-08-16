namespace RedmineCLI.Common.Exceptions;

/// <summary>
/// Exception thrown when validation of input data fails
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
