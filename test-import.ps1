$body = @{
    leagueIds = @('e94f549c-b586-4a22-9efd-8fd1f903ffce')
    seasons = @('2023-2024')
    includeWithoutOdds = $true
} | ConvertTo-Json

Write-Host "Starting import for Premier League 2023-2024..."
Write-Host "Body: $body"

$response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'

Write-Host "`nImport started!"
Write-Host "Job ID: $($response.jobId)"
Write-Host "Status: $($response.status)"

$response | ConvertTo-Json -Depth 10
