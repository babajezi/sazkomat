using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Sazkomat.Data.Scrapers;

public class ResilientHttpClient : IHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResilientHttpClient> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _retryPipeline;
    private readonly Random _random = new();

    private static readonly string[] UserAgents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
    };

    public ResilientHttpClient(HttpClient httpClient, ILogger<ResilientHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => !r.IsSuccessStatusCode),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Request failed. Retry {RetryCount} after {Delay}s. Reason: {Reason}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalSeconds,
                        args.Outcome.Exception?.Message ?? args.Outcome.Result?.ReasonPhrase ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<string> GetHtmlAsync(string url)
    {
        // Random delay between requests (2-5 seconds) to avoid rate limiting
        var delay = _random.Next(2000, 5001);
        await Task.Delay(delay);

        // Random user agent
        var userAgent = UserAgents[_random.Next(UserAgents.Length)];

        var response = await _retryPipeline.ExecuteAsync(async cancellationToken =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", userAgent);
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");

            _logger.LogInformation("Fetching: {Url}", url);
            var resp = await _httpClient.SendAsync(request, cancellationToken);
            resp.EnsureSuccessStatusCode();
            return resp;
        }, CancellationToken.None);

        var html = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Successfully fetched {Length} bytes from {Url}", html.Length, url);

        return html;
    }
}
