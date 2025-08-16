using System.Net;
using System.Net.Http;

using RedmineCLI.Common.Http;

using Xunit;

namespace RedmineCLI.Common.Tests.Http;

public class RedmineHttpClientFactoryRetryTests
{

    [Fact]
    public async Task RetryPolicy_RetriesOn500StatusCode()
    {
        // Arrange
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(null);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            if (callCount < 3)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, callCount);

    }

    [Fact]
    public async Task RetryPolicy_RetriesOn502BadGateway()
    {
        // Arrange
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(null);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task RetryPolicy_DoesNotRetryOn400BadRequest()
    {
        // Arrange
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(null);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(1, callCount);

    }

    [Fact]
    public async Task RetryPolicy_RetriesUpToMaxAttempts()
    {
        // Arrange
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(null);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(4, callCount); // 初回 + 3回のリトライ

    }

    [Fact]
    public async Task RetryPolicy_UsesExponentialBackoff()
    {
        // Arrange
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(null);
        var startTimes = new List<DateTime>();

        // Act
        await policy.ExecuteAsync(async () =>
        {
            startTimes.Add(DateTime.UtcNow);
            await Task.CompletedTask;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        });

        // Assert - 4 attempts total (1 initial + 3 retries)
        Assert.Equal(4, startTimes.Count);

        // Check that the delays follow exponential backoff pattern
        // We'll check that each delay is roughly the expected time
        for (int i = 1; i < startTimes.Count; i++)
        {
            var actualDelay = (startTimes[i] - startTimes[i - 1]).TotalSeconds;
            var expectedDelay = Math.Pow(2, i); // 2^1, 2^2, 2^3

            // Allow some tolerance for timing
            Assert.True(actualDelay >= expectedDelay - 0.5 && actualDelay <= expectedDelay + 1,
                $"Retry {i} delay was {actualDelay}s, expected ~{expectedDelay}s");
        }
    }

    [Fact]
    public async Task RetryPolicy_WorksWithoutLogger()
    {
        // Arrange
        var policy = RedmineHttpClientFactory.CreateRetryPolicy(null);
        var callCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            await Task.CompletedTask;
            if (callCount < 2)
            {
                return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, callCount);
    }

}
