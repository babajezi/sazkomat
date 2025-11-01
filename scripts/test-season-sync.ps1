# Test script for Season Sync functionality
$API_URL = "http://localhost:3001"
$PROVIDER_ID = "a0000000-0000-0000-0000-000000000001"

Write-Host "`n=== Testing Season Sync Workflow ===" -ForegroundColor Cyan

# Test 1: Check provider has current season patterns
Write-Host "`n1. Checking provider season patterns..." -ForegroundColor Yellow
$provider = Invoke-RestMethod -Uri "$API_URL/api/config/providers/$PROVIDER_ID"
Write-Host "   Provider: $($provider.name)" -ForegroundColor Green
Write-Host "   Current Season Patterns: $($provider.currentSeasonPatterns)" -ForegroundColor Green

# Test 2: Get a sample league
Write-Host "`n2. Getting sample league..." -ForegroundColor Yellow
$leagues = Invoke-RestMethod -Uri "$API_URL/api/config/leagues"
$sampleLeague = $leagues | Where-Object { $_.isEnabled -eq $true } | Select-Object -First 1
if ($sampleLeague) {
    Write-Host "   Sample League: $($sampleLeague.displayName)" -ForegroundColor Green
    Write-Host "   League ID: $($sampleLeague.id)" -ForegroundColor Green
}

# Test 3: Check league seasons before detection
Write-Host "`n3. Checking league seasons before detection..." -ForegroundColor Yellow
if ($sampleLeague) {
    $seasons = Invoke-RestMethod -Uri "$API_URL/api/config/seasons/league-seasons?leagueId=$($sampleLeague.id)"
    Write-Host "   Total seasons: $($seasons.Count)" -ForegroundColor Green
    if ($seasons.Count -gt 0) {
        $currentBefore = @($seasons | Where-Object { $_.isCurrent -eq $true })
        Write-Host "   Current seasons (before): $($currentBefore.Count)" -ForegroundColor Green
        $seasons | Select-Object -First 3 seasonName, isCurrent, syncMode, syncEnabled | Format-Table
    }
}

# Test 4: Detect current seasons
Write-Host "`n4. Detecting current seasons..." -ForegroundColor Yellow
try {
    $detectResult = Invoke-RestMethod -Uri "$API_URL/api/sync/seasons/detect-current" `
        -Method POST `
        -Body (@{providerId=$PROVIDER_ID} | ConvertTo-Json) `
        -ContentType "application/json"
    Write-Host "   Result: $($detectResult.message)" -ForegroundColor Green
} catch {
    Write-Host "   Error: $_" -ForegroundColor Red
}

# Test 5: Check league seasons after detection
Write-Host "`n5. Checking league seasons after detection..." -ForegroundColor Yellow
if ($sampleLeague) {
    Start-Sleep -Seconds 1
    $seasonsAfter = Invoke-RestMethod -Uri "$API_URL/api/config/seasons/league-seasons?leagueId=$($sampleLeague.id)"
    if ($seasonsAfter.Count -gt 0) {
        $currentAfter = @($seasonsAfter | Where-Object { $_.isCurrent -eq $true })
        Write-Host "   Current seasons (after): $($currentAfter.Count)" -ForegroundColor Green

        Write-Host "`n   Sample seasons:" -ForegroundColor Cyan
        $seasonsAfter | Select-Object -First 5 seasonName, isCurrent, syncMode, syncEnabled | Format-Table

        # Test 6: Toggle sync enabled for a current season
        $currentSeason = $currentAfter | Select-Object -First 1
        if ($currentSeason) {
            Write-Host "`n6. Testing sync toggle for season: $($currentSeason.seasonName)" -ForegroundColor Yellow
            try {
                Invoke-RestMethod -Uri "$API_URL/api/config/seasons/league-seasons/$($currentSeason.id)/sync-enabled" `
                    -Method PATCH `
                    -Body (@{enabled=$true} | ConvertTo-Json) `
                    -ContentType "application/json" | Out-Null
                Write-Host "   ✓ Sync enabled successfully" -ForegroundColor Green

                # Verify
                $verified = Invoke-RestMethod -Uri "$API_URL/api/config/seasons/league-seasons?leagueId=$($sampleLeague.id)"
                $verifiedSeason = $verified | Where-Object { $_.id -eq $currentSeason.id }
                Write-Host "   Sync status: $($verifiedSeason.syncEnabled)" -ForegroundColor Green
            } catch {
                Write-Host "   Error: $_" -ForegroundColor Red
            }
        }
    }
}

# Test 7: Test sync all marked seasons (dry run - will skip if no data)
Write-Host "`n7. Testing sync all marked seasons data..." -ForegroundColor Yellow
try {
    $syncResult = Invoke-RestMethod -Uri "$API_URL/api/sync/seasons/data" `
        -Method POST `
        -Body (@{providerId=$PROVIDER_ID} | ConvertTo-Json) `
        -ContentType "application/json" `
        -TimeoutSec 30
    Write-Host "   Result: $($syncResult.message)" -ForegroundColor Green
    Write-Host "   Statistics:" -ForegroundColor Cyan
    Write-Host "     Total Processed: $($syncResult.statistics.totalProcessed)"
    Write-Host "     Created: $($syncResult.statistics.created)"
    Write-Host "     Updated: $($syncResult.statistics.updated)"
    Write-Host "     Skipped: $($syncResult.statistics.skipped)"
    Write-Host "     Errors: $($syncResult.statistics.errors)"
} catch {
    Write-Host "   Error: $_" -ForegroundColor Red
}

Write-Host "`n=== Testing Complete ===" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Open http://localhost:3000/sync and test Step 4 buttons"
Write-Host "2. Open http://localhost:3000/leagues and expand seasons to see the new UI"
Write-Host "3. Toggle sync for specific seasons and test data synchronization"
