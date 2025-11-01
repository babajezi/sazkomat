# Download and analyze BetExplorer HTML structure
$url = "https://www.betexplorer.com/football/czech-republic/1-liga-2024-2025/results/"

Write-Host "Downloading HTML from BetExplorer..." -ForegroundColor Yellow
$response = Invoke-WebRequest -Uri $url -UserAgent "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"

# Save full HTML
$response.Content | Out-File "betexplorer-full.html" -Encoding UTF8

# Extract a sample match row
$html = $response.Content
if ($html -match '(?s)<table[^>]*table-main[^>]*>(.*?)</table>') {
    $tableContent = $matches[1]

    # Find first data row (after round header)
    if ($tableContent -match '(?s)<tr[^>]*data-dt="[^"]*"[^>]*>(.*?)</tr>') {
        $sampleRow = $matches[0]
        Write-Host "`nSample match row:" -ForegroundColor Green
        Write-Host $sampleRow

        # Count TD cells
        $tdMatches = [regex]::Matches($sampleRow, '<td[^>]*>')
        Write-Host "`nNumber of TD cells: $($tdMatches.Count)" -ForegroundColor Cyan

        # Extract each TD content
        $cellMatches = [regex]::Matches($sampleRow, '<td[^>]*>(.*?)</td>')
        for ($i = 0; $i -lt $cellMatches.Count; $i++) {
            $cellContent = $cellMatches[$i].Groups[1].Value -replace '<[^>]+>', '' -replace '\s+', ' '
            Write-Host "Cell $($i): '$cellContent'" -ForegroundColor White
        }
    }
}

Write-Host "`nFull HTML saved to: betexplorer-full.html" -ForegroundColor Green
