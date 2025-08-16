using RedmineCLI.Common.Exceptions;

using Xunit;

namespace RedmineCLI.Common.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void RedmineApiException_Constructor_SetsProperties()
    {
        // Arrange
        var statusCode = 404;
        var message = "Not Found";
        var apiError = "Resource not found";

        // Act
        var exception = new RedmineApiException(statusCode, message, apiError);

        // Assert
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(message, exception.Message);
        Assert.Equal(apiError, exception.ApiError);
    }

    [Fact]
    public void RedmineApiException_Constructor_WithoutApiError_SetsNullApiError()
    {
        // Arrange
        var statusCode = 500;
        var message = "Internal Server Error";

        // Act
        var exception = new RedmineApiException(statusCode, message);

        // Assert
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.ApiError);
    }

    [Fact]
    public void ValidationException_Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void ValidationException_Constructor_WithInnerException_SetsProperties()
    {
        // Arrange
        var message = "Validation failed";
        var innerException = new ArgumentException("Invalid argument");

        // Act
        var exception = new ValidationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Theory]
    [InlineData(400, "Bad Request", "Invalid parameters")]
    [InlineData(401, "Unauthorized", "Invalid API key")]
    [InlineData(403, "Forbidden", "Access denied")]
    [InlineData(404, "Not Found", null)]
    [InlineData(500, "Internal Server Error", "Database error")]
    public void RedmineApiException_VariousStatusCodes_WorkCorrectly(int statusCode, string message, string? apiError)
    {
        // Act
        var exception = new RedmineApiException(statusCode, message, apiError);

        // Assert
        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(message, exception.Message);
        Assert.Equal(apiError, exception.ApiError);
    }

    [Fact]
    public void RedmineApiException_IsException()
    {
        // Arrange & Act
        var exception = new RedmineApiException(500, "Error");

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void ValidationException_IsException()
    {
        // Arrange & Act
        var exception = new ValidationException("Error");

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void RedmineApiException_Serializable_Properties()
    {
        // Arrange
        var statusCode = 404;
        var message = "Not Found";
        var apiError = "Issue not found";
        var exception = new RedmineApiException(statusCode, message, apiError);

        // Act & Assert - Properties should be accessible
        Assert.NotNull(exception.ToString());
        Assert.Contains(message, exception.ToString());
    }

    [Fact]
    public void ValidationException_Serializable_Properties()
    {
        // Arrange
        var message = "Field is required";
        var exception = new ValidationException(message);

        // Act & Assert - Properties should be accessible
        Assert.NotNull(exception.ToString());
        Assert.Contains(message, exception.ToString());
    }
}
