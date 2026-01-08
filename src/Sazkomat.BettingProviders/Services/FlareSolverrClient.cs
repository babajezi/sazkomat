using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sazkomat.Core.Common;

namespace Sazkomat.BettingProviders.Services;

/// <summary>
/// Client for FlareSolverr - Cloudflare bypass proxy
/// </summary>
public class FlareSolverrClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FlareSolverrClient> _logger;
    private readonly string _flareSolverrUrl;
    private readonly int _maxTimeout;

    public FlareSolverrClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FlareSolverrClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _flareSolverrUrl = configuration["FlareSolverr:Url"] ?? "http://localhost:8191/v1";
        _maxTimeout = configuration.GetValue("FlareSolverr:MaxTimeout", 60000);
    }

    /// <summary>
    /// Fetches a URL through FlareSolverr, bypassing Cloudflare protection
    /// </summary>
    public async Task<Result<string>> GetAsync(string url, List<FlareSolverrCookie>? cookies = null)
    {
        try
        {
            _logger.LogInformation("FlareSolverr: Fetching {Url}", url);

            var request = new FlareSolverrRequest
            {
                Cmd = "request.get",
                Url = url,
                MaxTimeout = _maxTimeout,
                Cookies = cookies
            };

            var response = await _httpClient.PostAsJsonAsync(_flareSolverrUrl, request);
            var result = await response.Content.ReadFromJsonAsync<FlareSolverrResponse>();

            if (result?.Status != "ok")
            {
                _logger.LogError("FlareSolverr error: {Message}", result?.Message ?? "Unknown error");
                return Result<string>.Failure(result?.Message ?? "FlareSolverr request failed");
            }

            var content = result.Solution?.Response ?? "";

            // Extract JSON from HTML wrapper if present
            if (content.Contains("<pre>") && content.Contains("</pre>"))
            {
                var match = Regex.Match(content, @"<pre>({.*})</pre>", RegexOptions.Singleline);
                if (match.Success)
                {
                    content = match.Groups[1].Value;
                }
            }

            _logger.LogInformation("FlareSolverr: Got {Length} bytes, {CookieCount} cookies",
                content.Length, result.Solution?.Cookies?.Count ?? 0);

            return Result<string>.Success(content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "FlareSolverr HTTP error");
            return Result<string>.Failure($"FlareSolverr unavailable: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlareSolverr error");
            return Result<string>.Failure($"FlareSolverr error: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets session cookies by visiting a page
    /// </summary>
    public async Task<Result<List<FlareSolverrCookie>>> GetSessionCookiesAsync(string url)
    {
        try
        {
            _logger.LogInformation("FlareSolverr: Getting session cookies from {Url}", url);

            var request = new FlareSolverrRequest
            {
                Cmd = "request.get",
                Url = url,
                MaxTimeout = _maxTimeout
            };

            var response = await _httpClient.PostAsJsonAsync(_flareSolverrUrl, request);
            var result = await response.Content.ReadFromJsonAsync<FlareSolverrResponse>();

            if (result?.Status != "ok")
            {
                return Result<List<FlareSolverrCookie>>.Failure(result?.Message ?? "Failed to get cookies");
            }

            var cookies = result.Solution?.Cookies ?? new List<FlareSolverrCookie>();
            _logger.LogInformation("FlareSolverr: Got {Count} cookies", cookies.Count);

            return Result<List<FlareSolverrCookie>>.Success(cookies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlareSolverr cookie error");
            return Result<List<FlareSolverrCookie>>.Failure($"Cookie error: {ex.Message}");
        }
    }
}

#region FlareSolverr Models

public class FlareSolverrRequest
{
    [JsonPropertyName("cmd")]
    public string Cmd { get; set; } = "request.get";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("maxTimeout")]
    public int MaxTimeout { get; set; } = 60000;

    [JsonPropertyName("cookies")]
    public List<FlareSolverrCookie>? Cookies { get; set; }
}

public class FlareSolverrResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("solution")]
    public FlareSolverrSolution? Solution { get; set; }
}

public class FlareSolverrSolution
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("response")]
    public string? Response { get; set; }

    [JsonPropertyName("cookies")]
    public List<FlareSolverrCookie>? Cookies { get; set; }

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }
}

public class FlareSolverrCookie
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("expires")]
    public double? Expires { get; set; }

    [JsonPropertyName("httpOnly")]
    public bool HttpOnly { get; set; }

    [JsonPropertyName("secure")]
    public bool Secure { get; set; }

    [JsonPropertyName("sameSite")]
    public string? SameSite { get; set; }
}

#endregion
