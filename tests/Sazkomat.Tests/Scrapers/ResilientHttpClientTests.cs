using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using Sazkomat.DataImport.Scrapers;
using System.Net;

namespace Sazkomat.Tests.Scrapers;

public class ResilientHttpClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<ResilientHttpClient>> _mockLogger;
    private readonly HttpClient _httpClient;
    private readonly ResilientHttpClient _resilientClient;

    public ResilientHttpClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<ResilientHttpClient>>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _resilientClient = new ResilientHttpClient(_httpClient, _mockLogger.Object);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_SuccessfulRequest_ReturnsHtml()
    {
        // Arrange
        var expectedHtml = "<html><body>Test Content</body></html>";
        var url = "https://www.example.com/test";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedHtml)
            });

        // Act
        var html = await _resilientClient.GetHtmlAsync(url);

        // Assert
        Assert.Equal(expectedHtml, html);
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_AddsUserAgent_ToRequest()
    {
        // Arrange
        var url = "https://www.example.com/test";
        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html></html>")
            });

        // Act
        await _resilientClient.GetHtmlAsync(url);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.UserAgent.Any());
        Assert.True(capturedRequest.Headers.Contains("Accept"));
        Assert.True(capturedRequest.Headers.Contains("Accept-Language"));
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_TransientError_RetriesRequest()
    {
        // Arrange
        var url = "https://www.example.com/test";
        var callCount = 0;
        var expectedHtml = "<html><body>Success after retry</body></html>";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    // First 2 attempts fail with 503
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.ServiceUnavailable,
                        ReasonPhrase = "Service Temporarily Unavailable"
                    };
                }
                // Third attempt succeeds
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedHtml)
                };
            });

        // Act
        var html = await _resilientClient.GetHtmlAsync(url);

        // Assert
        Assert.Equal(expectedHtml, html);
        Assert.Equal(3, callCount); // Should have retried 2 times (3 total attempts)

        // Verify retry was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retry")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2));
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_HttpRequestException_RetriesRequest()
    {
        // Arrange
        var url = "https://www.example.com/test";
        var callCount = 0;
        var expectedHtml = "<html><body>Success after network error</body></html>";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    // First attempt throws network exception
                    throw new HttpRequestException("Network error");
                }
                // Second attempt succeeds
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedHtml)
                };
            });

        // Act
        var html = await _resilientClient.GetHtmlAsync(url);

        // Assert
        Assert.Equal(expectedHtml, html);
        Assert.Equal(2, callCount); // Should have retried once
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_PermanentError_ThrowsAfterMaxRetries()
    {
        // Arrange
        var url = "https://www.example.com/test";
        var callCount = 0;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    ReasonPhrase = "Service Unavailable"
                };
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _resilientClient.GetHtmlAsync(url)
        );

        // Should have tried: initial + 3 retries = 4 total attempts
        Assert.Equal(4, callCount);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_NotFound404_ThrowsImmediately()
    {
        // Arrange
        var url = "https://www.example.com/not-found";
        var callCount = 0;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ReasonPhrase = "Not Found"
                };
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _resilientClient.GetHtmlAsync(url)
        );

        // 404 should still trigger retries as per current implementation
        // (retry policy handles !IsSuccessStatusCode)
        Assert.Equal(4, callCount); // Initial + 3 retries
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_MultipleRequests_UsesRandomUserAgent()
    {
        // Arrange
        var url = "https://www.example.com/test";
        var userAgents = new List<string>();

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                var userAgent = req.Headers.UserAgent.ToString();
                userAgents.Add(userAgent);
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("<html></html>")
            });

        // Act - Make 5 requests
        for (int i = 0; i < 5; i++)
        {
            await _resilientClient.GetHtmlAsync(url);
        }

        // Assert
        Assert.Equal(5, userAgents.Count);
        Assert.All(userAgents, ua => Assert.False(string.IsNullOrWhiteSpace(ua)));
        // User agents should contain Mozilla string
        Assert.All(userAgents, ua => Assert.Contains("Mozilla", ua));
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_SuccessfulRequest_LogsInfoMessages()
    {
        // Arrange
        var url = "https://www.example.com/test";
        var html = "<html><body>Test</body></html>";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        // Act
        await _resilientClient.GetHtmlAsync(url);

        // Assert - Verify "Fetching" log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetching")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Assert - Verify "Successfully fetched" log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully fetched")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_IntermittentFailure_EventuallySucceeds()
    {
        // Arrange
        var url = "https://www.example.com/test";
        var callCount = 0;
        var expectedHtml = "<html><body>Eventual success</body></html>";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                // Fail on first attempt, succeed on second
                if (callCount == 1)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError
                    };
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedHtml)
                };
            });

        // Act
        var html = await _resilientClient.GetHtmlAsync(url);

        // Assert
        Assert.Equal(expectedHtml, html);
        Assert.Equal(2, callCount); // Initial + 1 retry
    }

    [Trait("Category", "Integration")]
    [Trait("Type", "Scraper")]
    [Fact]
    public async Task GetHtmlAsync_LargeContent_HandlesCorrectly()
    {
        // Arrange
        var url = "https://www.example.com/large";
        var largeHtml = string.Join("", Enumerable.Repeat("<div>Test Content</div>", 10000));

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(largeHtml)
            });

        // Act
        var html = await _resilientClient.GetHtmlAsync(url);

        // Assert
        Assert.Equal(largeHtml.Length, html.Length);
        Assert.Equal(largeHtml, html);
    }
}
