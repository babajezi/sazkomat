// Jednoduchý C# script pro analýzu Tipsport stránky
// Spustit: dotnet script analyze-tipsport.cs (potřebuje dotnet-script tool)
// Nebo jako součást testů

using Microsoft.Playwright;
using System.Text.Json;
using System.Text.RegularExpressions;

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = true,
    Args = new[] { "--disable-blink-features=AutomationControlled" }
});

var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
});

var page = await context.NewPageAsync();

// Zachytit všechny API requesty
var apiRequests = new List<(string Url, string? Response)>();

page.Response += async (_, response) =>
{
    var url = response.Url;
    if (url.Contains("api") || url.Contains("json") || url.Contains("graphql") ||
        response.Headers.ContainsKey("content-type") && response.Headers["content-type"].Contains("application/json"))
    {
        try
        {
            var body = await response.TextAsync();
            apiRequests.Add((url, body.Length > 1000 ? body.Substring(0, 1000) + "..." : body));
            Console.WriteLine($"[API] {url} - {body.Length} bytes");
        }
        catch
        {
            apiRequests.Add((url, null));
        }
    }
};

Console.WriteLine("Navigating to Tipsport...");
await page.GotoAsync("https://www.tipsport.cz/vysledky", new PageGotoOptions
{
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = 60000
});

Console.WriteLine("Page loaded, waiting for content...");
await Task.Delay(5000);

// Klikni na Fotbal
try
{
    var fotbalLink = await page.WaitForSelectorAsync("text=Fotbal", new PageWaitForSelectorOptions { Timeout = 10000 });
    if (fotbalLink != null)
    {
        await fotbalLink.ClickAsync();
        Console.WriteLine("Clicked on Fotbal");
        await Task.Delay(3000);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Could not click Fotbal: {ex.Message}");
}

// Získej HTML
var html = await page.ContentAsync();
Console.WriteLine($"\nHTML size: {html.Length} bytes");

// Hledej JSON v HTML
var jsonPatterns = new[]
{
    @"window\.__INITIAL_STATE__\s*=\s*(\{.+?\});",
    @"window\[""initial_state""\]\s*=\s*(\{.+?\})\s*</script>",
    @"<script[^>]*type=""application/json""[^>]*>(\{.+?\})</script>",
    @"data-state=""([^""]+)""",
};

Console.WriteLine("\n=== Searching for embedded JSON ===");
foreach (var pattern in jsonPatterns)
{
    var match = Regex.Match(html, pattern, RegexOptions.Singleline);
    if (match.Success)
    {
        Console.WriteLine($"Found JSON with pattern: {pattern.Substring(0, 30)}...");
        Console.WriteLine($"JSON preview: {match.Groups[1].Value.Substring(0, Math.Min(500, match.Groups[1].Value.Length))}...");
    }
}

// Vypsat zachycené API requesty
Console.WriteLine($"\n=== Captured {apiRequests.Count} API requests ===");
foreach (var (url, response) in apiRequests.Take(20))
{
    Console.WriteLine($"\nURL: {url}");
    if (response != null)
    {
        Console.WriteLine($"Response preview: {response}");
    }
}

// Najdi ligy v HTML
Console.WriteLine("\n=== Looking for league elements ===");
var leagueElements = await page.QuerySelectorAllAsync("[class*='league'], [class*='competition'], [data-sport]");
Console.WriteLine($"Found {leagueElements.Count} potential league elements");

// Ulož HTML pro pozdější analýzu
await File.WriteAllTextAsync("/tmp/tipsport_vysledky.html", html);
Console.WriteLine("\nSaved HTML to /tmp/tipsport_vysledky.html");

// Ulož API responses
var apiJson = JsonSerializer.Serialize(apiRequests.Select(r => new { r.Url, ResponsePreview = r.Response }), new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync("/tmp/tipsport_api_requests.json", apiJson);
Console.WriteLine("Saved API requests to /tmp/tipsport_api_requests.json");

await browser.CloseAsync();
playwright.Dispose();

Console.WriteLine("\nDone!");
