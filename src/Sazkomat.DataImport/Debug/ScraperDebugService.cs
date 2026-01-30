using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Sazkomat.DataImport.Debug;

/// <summary>
/// Service for debugging web scraping with step-by-step execution and detailed logging
/// </summary>
public class ScraperDebugService : IAsyncDisposable
{
    private readonly ILogger<ScraperDebugService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private readonly List<string> _logs = new();
    private readonly Stopwatch _sessionStopwatch = new();
    private bool _initialized = false;

    private static readonly string DebugDirectory = "/app/debug";

    private static readonly string[] UserAgents = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0"
    };

    public ScraperDebugService(ILogger<ScraperDebugService> logger)
    {
        _logger = logger;
    }

    public async Task<DebugSessionResult> ExecuteAsync(DebugRequest request)
    {
        _sessionStopwatch.Restart();
        _logs.Clear();
        Log("Starting debug session");

        try
        {
            await InitializeBrowserAsync();

            var results = new List<DebugStepResult>();

            foreach (var (action, index) in request.Actions.Select((a, i) => (a, i + 1)))
            {
                var stepResult = await ExecuteActionAsync(action, index);
                results.Add(stepResult);

                if (!stepResult.Success)
                {
                    Log($"Step {index} failed: {stepResult.Error}");
                    break;
                }
            }

            return new DebugSessionResult
            {
                Success = results.All(r => r.Success),
                TotalDurationMs = _sessionStopwatch.ElapsedMilliseconds,
                Results = results,
                Logs = _logs.ToList()
            };
        }
        catch (Exception ex)
        {
            Log($"Session error: {ex.Message}");
            return new DebugSessionResult
            {
                Success = false,
                TotalDurationMs = _sessionStopwatch.ElapsedMilliseconds,
                Results = new List<DebugStepResult>(),
                Logs = _logs.ToList()
            };
        }
    }

    private async Task InitializeBrowserAsync()
    {
        if (_initialized) return;

        Log("Initializing Playwright browser...");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        var userAgent = UserAgents[Random.Shared.Next(UserAgents.Length)];

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        _page = await _context.NewPageAsync();

        _initialized = true;
        Log("Browser initialized successfully");

        // Ensure debug directory exists
        Directory.CreateDirectory(DebugDirectory);
    }

    private async Task<DebugStepResult> ExecuteActionAsync(DebugAction action, int step)
    {
        var sw = Stopwatch.StartNew();
        Log($"Step {step}: {action.ActionType}");

        try
        {
            var details = action switch
            {
                NavigateAction nav => await ExecuteNavigate(nav),
                WaitAction wait => await ExecuteWait(wait),
                WaitForSelectorAction wfs => await ExecuteWaitForSelector(wfs),
                WaitForLoadStateAction wls => await ExecuteWaitForLoadState(wls),
                ClickAction click => await ExecuteClick(click),
                TypeTextAction type => await ExecuteType(type),
                SelectAction select => await ExecuteSelect(select),
                ScreenshotAction screenshot => await ExecuteScreenshot(screenshot),
                LogElementsAction log => await ExecuteLogElements(log),
                ExtractHtmlAction extract => await ExecuteExtractHtml(extract),
                EvaluateAction eval => await ExecuteEvaluate(eval),
                ScrollAction scroll => await ExecuteScroll(scroll),
                _ => throw new ArgumentException($"Unknown action type: {action.ActionType}")
            };

            Log($"Step {step} completed in {sw.ElapsedMilliseconds}ms");

            return new DebugStepResult
            {
                Step = step,
                Action = action.ActionType,
                Success = true,
                DurationMs = sw.ElapsedMilliseconds,
                Details = details
            };
        }
        catch (Exception ex)
        {
            Log($"Step {step} error: {ex.Message}");

            return new DebugStepResult
            {
                Step = step,
                Action = action.ActionType,
                Success = false,
                DurationMs = sw.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }

    private async Task<NavigateDetails> ExecuteNavigate(NavigateAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Navigating to: {action.Url}");

        await _page.GotoAsync(action.Url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 30000
        });

        var finalUrl = _page.Url;
        Log($"Navigation complete, URL: {finalUrl}");

        return new NavigateDetails { FinalUrl = finalUrl };
    }

    private async Task<object?> ExecuteWait(WaitAction action)
    {
        Log($"Waiting {action.Milliseconds}ms...");
        await Task.Delay(action.Milliseconds);
        return null;
    }

    private async Task<WaitForSelectorDetails> ExecuteWaitForSelector(WaitForSelectorAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Waiting for selector: {action.Selector}");

        var state = action.State?.ToLower() switch
        {
            "visible" => WaitForSelectorState.Visible,
            "hidden" => WaitForSelectorState.Hidden,
            "attached" => WaitForSelectorState.Attached,
            "detached" => WaitForSelectorState.Detached,
            _ => WaitForSelectorState.Visible
        };

        var element = await _page.WaitForSelectorAsync(action.Selector, new PageWaitForSelectorOptions
        {
            Timeout = action.Timeout,
            State = state
        });

        var tag = element != null
            ? await element.EvaluateAsync<string>("el => el.tagName.toLowerCase()")
            : null;

        Log($"Selector found: {tag ?? "null"}");

        return new WaitForSelectorDetails
        {
            Found = element != null,
            ElementTag = tag
        };
    }

    private async Task<object?> ExecuteWaitForLoadState(WaitForLoadStateAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        var state = action.State.ToLower() switch
        {
            "load" => LoadState.Load,
            "domcontentloaded" => LoadState.DOMContentLoaded,
            "networkidle" => LoadState.NetworkIdle,
            _ => LoadState.NetworkIdle
        };

        Log($"Waiting for load state: {action.State}");

        await _page.WaitForLoadStateAsync(state, new PageWaitForLoadStateOptions
        {
            Timeout = action.Timeout
        });

        Log("Load state reached");
        return null;
    }

    private async Task<object?> ExecuteClick(ClickAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Clicking: {action.Selector}");

        await _page.ClickAsync(action.Selector);

        Log("Click completed");
        return null;
    }

    private async Task<object?> ExecuteType(TypeTextAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Typing into: {action.Selector}");

        await _page.FillAsync(action.Selector, action.Text);

        Log("Type completed");
        return null;
    }

    private async Task<object?> ExecuteSelect(SelectAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Selecting value '{action.Value}' in: {action.Selector}");

        await _page.SelectOptionAsync(action.Selector, action.Value);

        Log("Select completed");
        return null;
    }

    private async Task<ScreenshotDetails> ExecuteScreenshot(ScreenshotAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        var filename = $"{action.Name}.png";
        var path = Path.Combine(DebugDirectory, filename);

        Log($"Taking screenshot: {path}");

        await _page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });

        var viewport = _page.ViewportSize;

        Log($"Screenshot saved: {path}");

        return new ScreenshotDetails
        {
            Path = path,
            Width = viewport?.Width ?? 0,
            Height = viewport?.Height ?? 0
        };
    }

    private async Task<LogElementsDetails> ExecuteLogElements(LogElementsAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Logging elements: {action.Selector} (limit: {action.Limit})");

        var elements = await _page.QuerySelectorAllAsync(action.Selector);
        var count = elements.Count;

        Log($"Found {count} elements matching '{action.Selector}'");

        var elementInfos = new List<ElementInfo>();

        foreach (var element in elements.Take(action.Limit))
        {
            var info = new ElementInfo();

            info.Tag = await element.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
            info.Id = await element.EvaluateAsync<string?>("el => el.id || null");
            info.Class = await element.EvaluateAsync<string?>("el => el.className || null");

            if (action.ExtractText)
            {
                info.Text = await element.EvaluateAsync<string?>("el => el.textContent?.trim()?.substring(0, 200) || null");
            }

            if (action.Attributes != null && action.Attributes.Count > 0)
            {
                info.Attributes = new Dictionary<string, string>();
                foreach (var attr in action.Attributes)
                {
                    var value = await element.EvaluateAsync<string?>($"el => el.getAttribute('{attr}')");
                    if (value != null)
                    {
                        info.Attributes[attr] = value;
                    }
                }
            }

            elementInfos.Add(info);

            // Log each element for debugging
            var logText = $"  [{info.Tag}] id={info.Id ?? "(none)"} class={info.Class ?? "(none)"}";
            if (action.ExtractText && info.Text != null)
            {
                logText += $" text=\"{info.Text.Substring(0, Math.Min(50, info.Text.Length))}...\"";
            }
            Log(logText);
        }

        return new LogElementsDetails
        {
            Count = count,
            Elements = elementInfos
        };
    }

    private async Task<ExtractHtmlDetails> ExecuteExtractHtml(ExtractHtmlAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        string html;

        if (string.IsNullOrEmpty(action.Selector))
        {
            Log("Extracting full page HTML");
            html = await _page.ContentAsync();
        }
        else
        {
            Log($"Extracting HTML from: {action.Selector}");
            var element = await _page.QuerySelectorAsync(action.Selector);
            if (element == null)
            {
                throw new Exception($"Element not found: {action.Selector}");
            }
            html = await element.EvaluateAsync<string>("el => el.outerHTML");
        }

        var truncated = html.Length > action.MaxLength;
        if (truncated)
        {
            html = html.Substring(0, action.MaxLength);
            Log($"HTML truncated from {html.Length} to {action.MaxLength} chars");
        }

        Log($"Extracted {html.Length} chars of HTML");

        return new ExtractHtmlDetails
        {
            Html = html,
            Length = html.Length,
            Truncated = truncated
        };
    }

    private async Task<EvaluateDetails> ExecuteEvaluate(EvaluateAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Evaluating JavaScript: {action.Script.Substring(0, Math.Min(100, action.Script.Length))}...");

        var result = await _page.EvaluateAsync<object?>(action.Script);

        Log($"JavaScript result: {result?.ToString()?.Substring(0, Math.Min(200, result.ToString()?.Length ?? 0))}");

        return new EvaluateDetails { Result = result };
    }

    private async Task<object?> ExecuteScroll(ScrollAction action)
    {
        if (_page == null) throw new InvalidOperationException("Page not initialized");

        Log($"Scrolling: {action.Direction}");

        var script = action.Direction.ToLower() switch
        {
            "top" => "window.scrollTo(0, 0)",
            "bottom" => "window.scrollTo(0, document.body.scrollHeight)",
            "up" => $"window.scrollBy(0, -{action.Pixels ?? 500})",
            "down" => $"window.scrollBy(0, {action.Pixels ?? 500})",
            _ => throw new ArgumentException($"Invalid scroll direction: {action.Direction}")
        };

        await _page.EvaluateAsync(script);

        Log("Scroll completed");
        return null;
    }

    private void Log(string message)
    {
        var timestamp = _sessionStopwatch.Elapsed;
        var logEntry = $"[{timestamp:mm\\:ss\\.fff}] {message}";
        _logs.Add(logEntry);
        _logger.LogDebug(logEntry);
    }

    public async ValueTask DisposeAsync()
    {
        if (_context != null)
        {
            await _context.CloseAsync();
            _context = null;
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        _page = null;
        _initialized = false;

        GC.SuppressFinalize(this);
    }
}
