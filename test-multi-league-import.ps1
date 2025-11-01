# Test importing multiple leagues simultaneously

$leagues = @(
    @{name="La Liga"; id="f22c1a9c-4528-4d20-aa26-c139d3d2afc1"},
    @{name="Bundesliga"; id="5b040f3a-95b6-472a-ab7d-235c0e572183"},
    @{name="Serie A"; id="2b6666dd-7dc8-4ef8-88ba-46d03d847aab"},
    @{name="Ligue 1"; id="edaec3ad-c419-420c-aae3-5e175893957e"}
)

$leagueIds = $leagues | ForEach-Object { $_.id }

$body = @{
    leagueIds = $leagueIds
    seasons = @('2023-2024')
    includeWithoutOdds = $true
} | ConvertTo-Json

Write-Host ""
Write-Host "Starting multi-league import (4 leagues)..."
Write-Host "============================================"
Write-Host "Leagues: La Liga, Bundesliga, Serie A, Ligue 1"
Write-Host "Season: 2023-2024"
Write-Host ""

$startTime = Get-Date

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'

    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    Write-Host "Import jobs started successfully!" -ForegroundColor Green
    Write-Host "Job ID: $($response.jobId)"
    Write-Host "Request time: $([math]::Round($duration, 2)) seconds"
    Write-Host ""

    # Wait a bit and check status
    Write-Host "Waiting 15 seconds for import to process..."
    Start-Sleep -Seconds 15

    Write-Host ""
    Write-Host "Checking job status..."
    $jobStatus = Invoke-RestMethod -Uri "http://localhost:3001/api/import/jobs/$($response.jobId)"

    Write-Host "Status: $($jobStatus.status) (0=Pending, 1=Running, 2=Completed, 3=Failed)"
    Write-Host "Processed Rounds: $($jobStatus.progress.processedRounds)"
    Write-Host "Total Seasons: $($jobStatus.progress.totalSeasons)"

    if ($jobStatus.status -eq 2) {
        Write-Host ""
        Write-Host "Import completed successfully!" -ForegroundColor Green

        $importTime = ($jobStatus.completedAt - $jobStatus.startedAt)
        Write-Host "Total import time: $([math]::Round($importTime.TotalSeconds, 2)) seconds"
    } else {
        Write-Host ""
        Write-Host "Import still in progress. Check status later with:"
        Write-Host "  curl http://localhost:3001/api/import/jobs/$($response.jobId)"
    }

} catch {
    Write-Host "Error starting import: $_" -ForegroundColor Red
}
