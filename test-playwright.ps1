# Test Playwright HTML structure
$url = "https://www.betexplorer.com/football/czech-republic/chnl-2024-2025/results/"

Write-Host "Starting Playwright test..." -ForegroundColor Yellow
Write-Host "URL: $url" -ForegroundColor Cyan

# Use PlaywrightSharp CLI
playwright codegen --target csharp -o test-output.html $url

Write-Host "`nOpening browser and saving HTML..." -ForegroundColor Yellow
Write-Host "This will open a browser window. Once page loads, close the browser." -ForegroundColor Cyan
Write-Host "HTML will be saved to 'test-output.html'" -ForegroundColor Green
