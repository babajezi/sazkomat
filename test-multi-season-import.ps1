# Test importing multiple seasons for Premier League

$body = @{
    leagueIds = @('e94f549c-b586-4a22-9efd-8fd1f903ffce')  # Premier League
    seasons = @('2022-2023', '2021-2022', '2020-2021')
    includeWithoutOdds = $true
} | ConvertTo-Json

Write-Host ""
Write-Host "Starting multi-season import..."
Write-Host "==============================="
Write-Host "League: Premier League"
Write-Host "Seasons: 2022-2023, 2021-2022, 2020-2021"
Write-Host ""

$startTime = Get-Date

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'

    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    Write-Host "Import job started successfully!" -ForegroundColor Green
    Write-Host "Job ID: $($response.jobId)"
    Write-Host "Request time: $([math]::Round($duration, 2)) seconds"
    Write-Host ""

    # Wait for import to complete
    Write-Host "Waiting for import to complete..."
    $maxWait = 30
    $waited = 0
    $completed = $false

    while ($waited -lt $maxWait) {
        Start-Sleep -Seconds 3
        $waited += 3

        $jobStatus = Invoke-RestMethod -Uri "http://localhost:3001/api/import/jobs/$($response.jobId)"

        Write-Host "  [$waited`s] Status: $($jobStatus.status), Rounds: $($jobStatus.progress.processedRounds)"

        if ($jobStatus.status -eq 2) {
            $completed = $true
            break
        }

        if ($jobStatus.status -eq 3) {
            Write-Host ""
            Write-Host "Import failed!" -ForegroundColor Red
            Write-Host "Errors: $($jobStatus.progress.errors -join ', ')"
            exit 1
        }
    }

    if ($completed) {
        Write-Host ""
        Write-Host "Import completed successfully!" -ForegroundColor Green
        Write-Host "Total rounds processed: $($jobStatus.progress.processedRounds)"
        Write-Host "Seasons imported: $($jobStatus.progress.totalSeasons)"
        Write-Host ""

        # Get detailed stats
        Write-Host "Fetching detailed statistics..."
        $stats = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/stats?leagueId=e94f549c-b586-4a22-9efd-8fd1f903ffce'

        Write-Host ""
        Write-Host "Premier League Statistics:"
        Write-Host "--------------------------"
        Write-Host "Total Rounds: $($stats.totalRounds)"
        Write-Host "Total Seasons: $($stats.totalSeasons)"
        Write-Host ""
        Write-Host "Rounds by Season:"
        foreach ($season in $stats.roundsBySeason.PSObject.Properties) {
            Write-Host "  $($season.Name): $($season.Value) rounds"
        }
    } else {
        Write-Host ""
        Write-Host "Import still running after ${maxWait}s. Check status later." -ForegroundColor Yellow
    }

} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
