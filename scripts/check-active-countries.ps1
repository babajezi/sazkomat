$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'
$active = $countries | Where-Object { $_.isActive -eq $true }

Write-Host "Total countries: $($countries.Count)"
Write-Host "Active countries: $($active.Count)"

if ($active.Count -gt 0) {
    Write-Host "`nActive countries:"
    $active | ForEach-Object { Write-Host "  - $($_.name)" }
} else {
    Write-Host "`nNo active countries found!" -ForegroundColor Yellow
}
