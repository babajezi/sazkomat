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

            // Filter out cookies with invalid expiry (null or 0) to avoid Chrome errors
            var sanitizedCookies = cookies?.Select(c => new FlareSolverrCookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = c.Domain,
                Path = c.Path,
                Expiry = c.Expiry > 0 ? c.Expiry : null, // Only include valid expiry
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = c.SameSite
            }).ToList();

            var request = new FlareSolverrRequest
            {
                Cmd = "request.get",
                Url = url,
                MaxTimeout = _maxTimeout,
                Cookies = sanitizedCookies
            };

            // Use custom serialization to skip null values
            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request, jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_flareSolverrUrl, jsonContent);
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

            // Debug: Log raw response
            var rawContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("FlareSolverr raw response length: {Length}", rawContent.Length);

            var result = JsonSerializer.Deserialize<FlareSolverrResponse>(rawContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Status != "ok")
            {
                _logger.LogWarning("FlareSolverr status not ok: {Status}, message: {Message}", result?.Status, result?.Message);
                return Result<List<FlareSolverrCookie>>.Failure(result?.Message ?? "Failed to get cookies");
            }

            var cookies = result.Solution?.Cookies ?? new List<FlareSolverrCookie>();
            _logger.LogInformation("FlareSolverr: Got {Count} cookies (Solution null: {SolutionNull}, Cookies null: {CookiesNull})",
                cookies.Count,
                result.Solution == null,
                result.Solution?.Cookies == null);

            return Result<List<FlareSolverrCookie>>.Success(cookies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlareSolverr cookie error");
            return Result<List<FlareSolverrCookie>>.Failure($"Cookie error: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a persistent FlareSolverr session
    /// </summary>
    public async Task<Result<string>> CreateSessionAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("FlareSolverr: Creating session {SessionId}", sessionId);

            var request = new { cmd = "sessions.create", session = sessionId };
            var response = await _httpClient.PostAsJsonAsync(_flareSolverrUrl, request);
            var result = await response.Content.ReadFromJsonAsync<FlareSolverrResponse>();

            if (result?.Status != "ok")
            {
                // Session might already exist, which is fine
                if (result?.Message?.Contains("already exists") == true)
                {
                    _logger.LogDebug("FlareSolverr session {SessionId} already exists", sessionId);
                    return Result<string>.Success(sessionId);
                }
                _logger.LogWarning("FlareSolverr session create failed: {Message}", result?.Message);
                return Result<string>.Failure(result?.Message ?? "Failed to create session");
            }

            _logger.LogInformation("FlareSolverr: Session {SessionId} created", sessionId);
            return Result<string>.Success(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlareSolverr session create error");
            return Result<string>.Failure($"Session create error: {ex.Message}");
        }
    }

    /// <summary>
    /// Destroys a FlareSolverr session
    /// </summary>
    public async Task<Result> DestroySessionAsync(string sessionId)
    {
        try
        {
            _logger.LogDebug("FlareSolverr: Destroying session {SessionId}", sessionId);

            var request = new { cmd = "sessions.destroy", session = sessionId };
            var response = await _httpClient.PostAsJsonAsync(_flareSolverrUrl, request);
            var result = await response.Content.ReadFromJsonAsync<FlareSolverrResponse>();

            if (result?.Status != "ok")
            {
                _logger.LogWarning("FlareSolverr session destroy warning: {Message}", result?.Message);
                // Not a failure - session might not exist
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FlareSolverr session destroy error (non-critical)");
            return Result.Success(); // Non-critical error
        }
    }

    /// <summary>
    /// Fetches a URL using a persistent FlareSolverr session
    /// </summary>
    public async Task<Result<string>> GetWithSessionAsync(string url, string sessionId)
    {
        try
        {
            _logger.LogInformation("FlareSolverr: Fetching {Url} with session {SessionId}", url, sessionId);

            var request = new FlareSolverrSessionRequest
            {
                Cmd = "request.get",
                Url = url,
                Session = sessionId,
                MaxTimeout = _maxTimeout
            };

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(request, jsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_flareSolverrUrl, jsonContent);
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

            _logger.LogInformation("FlareSolverr: Got {Length} bytes with session {SessionId}",
                content.Length, sessionId);

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

public class FlareSolverrSessionRequest
{
    [JsonPropertyName("cmd")]
    public string Cmd { get; set; } = "request.get";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("session")]
    public string Session { get; set; } = "";

    [JsonPropertyName("maxTimeout")]
    public int MaxTimeout { get; set; } = 60000;
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

    [JsonPropertyName("expiry")]
    public double? Expiry { get; set; }

    // Legacy alias for backwards compatibility
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
