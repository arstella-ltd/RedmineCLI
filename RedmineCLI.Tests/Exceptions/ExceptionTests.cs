using FluentAssertions;

using RedmineCLI.Exceptions;

using Xunit;

namespace RedmineCLI.Tests.Exceptions;

public class ExceptionTests
{
    [Fact]
    public void RedmineApiException_Should_HaveCorrectProperties_When_Created()
    {
        // Arrange
        var statusCode = 404;
        var message = "API error occurred";
        var apiError = "Not Found";

        // Act
        var exception = new RedmineApiException(statusCode, message, apiError);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(statusCode);
        exception.ApiError.Should().Be(apiError);
    }

    [Fact]
    public void RedmineApiException_Should_HaveNullApiError_When_NotProvided()
    {
        // Arrange
        var statusCode = 500;
        var message = "Internal Server Error";

        // Act
        var exception = new RedmineApiException(statusCode, message);

        // Assert
        exception.Message.Should().Be(message);
        exception.StatusCode.Should().Be(statusCode);
        exception.ApiError.Should().BeNull();
    }

    [Fact]
    public void ValidationException_Should_HaveCorrectMessage_When_CreatedWithMessage()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void ValidationException_Should_HaveCorrectMessageAndInnerException_When_CreatedWithInnerException()
    {
        // Arrange
        var message = "Validation failed";
        var innerException = new System.Exception("Inner validation error");

        // Act
        var exception = new ValidationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void RedmineApiException_Should_BeOfTypeException()
    {
        // Arrange & Act
        var exception = new RedmineApiException(400, "Bad Request");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ValidationException_Should_BeOfTypeException()
    {
        // Arrange & Act
        var exception = new ValidationException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void RedmineApiException_Should_BeThrowable()
    {
        // Arrange
        Action act = () => throw new RedmineApiException(401, "Unauthorized", "Invalid API key");

        // Act & Assert
        act.Should().Throw<RedmineApiException>()
            .WithMessage("Unauthorized")
            .And.StatusCode.Should().Be(401);
    }

    [Fact]
    public void ValidationException_Should_BeThrowable()
    {
        // Arrange
        Action act = () => throw new ValidationException("Validation error");

        // Act & Assert
        act.Should().Throw<ValidationException>()
            .WithMessage("Validation error");
    }

    [Fact]
    public void RedmineApiException_Should_HaveCorrectStatusCode_When_ThrowingDifferentErrors()
    {
        // Arrange
        var exceptions = new[]
        {
            new RedmineApiException(400, "Bad Request"),
            new RedmineApiException(401, "Unauthorized"),
            new RedmineApiException(403, "Forbidden"),
            new RedmineApiException(404, "Not Found"),
            new RedmineApiException(500, "Internal Server Error")
        };

        // Act & Assert
        exceptions[0].StatusCode.Should().Be(400);
        exceptions[1].StatusCode.Should().Be(401);
        exceptions[2].StatusCode.Should().Be(403);
        exceptions[3].StatusCode.Should().Be(404);
        exceptions[4].StatusCode.Should().Be(500);
    }
}