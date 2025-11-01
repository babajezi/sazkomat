$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'

Write-Host "Total countries: $($countries.Count)"
Write-Host ""
Write-Host "First 15 countries (should be alphabetically sorted):"
$countries | Select-Object -First 15 | ForEach-Object { Write-Host "  $_($_.name)" }

Write-Host ""
Write-Host "Checking for continents..."
$continentCodes = @('europe', 'africa', 'asia', 'australia-oceania', 'oceania', 'north-central-america', 'south-america', 'world')
$continents = $countries | Where-Object { $continentCodes -contains $_.code }

if ($continents.Count -gt 0) {
    Write-Host "ERROR: Found $($continents.Count) continents:" -ForegroundColor Red
    $continents | ForEach-Object { Write-Host "  - $($_.name) ($($_.code))" -ForegroundColor Red }
} else {
    Write-Host "OK: No continents found!" -ForegroundColor Green
}
