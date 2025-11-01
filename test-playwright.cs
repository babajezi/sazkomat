using Microsoft.Playwright;
using HtmlAgilityPack;

var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
var page = await browser.NewPageAsync();

Console.WriteLine("Loading page...");
await page.GotoAsync("https://www.betexplorer.com/football/czech-republic/chnl-2024-2025/results/",
    new() { WaitUntil = WaitUntilState.NetworkIdle });

Console.WriteLine("Waiting for results container...");
await page.WaitForSelectorAsync("#js-leagueresults-all", new() { Timeout = 10000 });

var html = await page.ContentAsync();
Console.WriteLine($"HTML size: {html.Length} bytes");

// Parse first match row
var doc = new HtmlDocument();
doc.LoadHtml(html);

var rows = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-main')]//tr[contains(@data-dt, '')]");
if (rows != null && rows.Count > 0)
{
    var firstRow = rows[0];
    Console.WriteLine("\nFirst match row attributes:");
    foreach (var attr in firstRow.Attributes)
    {
        Console.WriteLine($"  {attr.Name} = {attr.Value}");
    }

    var cells = firstRow.SelectNodes(".//td");
    if (cells != null)
    {
        Console.WriteLine($"\nNumber of TD cells: {cells.Count}");
        for (int i = 0; i < cells.Count; i++)
        {
            var cellText = cells[i].InnerText.Trim().Replace("\n", " ").Replace("\r", "");
            var cellClass = cells[i].GetAttributeValue("class", "");
            Console.WriteLine($"Cell {i}: class='{cellClass}' text='{cellText}'");
        }
    }
}

await browser.CloseAsync();
Console.WriteLine("\nDone!");
