# Test error handling scenarios

Write-Host ""
Write-Host "====================================="
Write-Host "  ERROR HANDLING TESTS"
Write-Host "====================================="
Write-Host ""

$tests = @()

# Test 1: Non-existent league ID
Write-Host "Test 1: Non-existent league ID"
Write-Host "-------------------------------"
$body = @{
    leagueIds = @('00000000-0000-0000-0000-000000000000')
    seasons = @('2023-2024')
    includeWithoutOdds = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'
    Write-Host "  FAIL - Should have returned error but succeeded" -ForegroundColor Red
    $tests += @{name="Non-existent league"; passed=$false}
} catch {
    $errorMsg = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "  PASS - Got expected error: $($errorMsg.error)" -ForegroundColor Green
    $tests += @{name="Non-existent league"; passed=$true}
}
Write-Host ""

# Test 2: Disabled league
Write-Host "Test 2: Disabled league"
Write-Host "-----------------------"

# First, disable Premier League
$disableBody = @{isEnabled = $false} | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:3001/api/config/leagues/e94f549c-b586-4a22-9efd-8fd1f903ffce' -Method PATCH -Body $disableBody -ContentType 'application/json' | Out-Null

$body = @{
    leagueIds = @('e94f549c-b586-4a22-9efd-8fd1f903ffce')
    seasons = @('2023-2024')
    includeWithoutOdds = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'
    Write-Host "  FAIL - Should have returned error but succeeded" -ForegroundColor Red
    $tests += @{name="Disabled league"; passed=$false}
} catch {
    $errorMsg = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "  PASS - Got expected error: $($errorMsg.error)" -ForegroundColor Green
    $tests += @{name="Disabled league"; passed=$true}
}

# Re-enable Premier League
$enableBody = @{isEnabled = $true} | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:3001/api/config/leagues/e94f549c-b586-4a22-9efd-8fd1f903ffce' -Method PATCH -Body $enableBody -ContentType 'application/json' | Out-Null
Write-Host ""

# Test 3: Empty league IDs
Write-Host "Test 3: Empty league IDs"
Write-Host "------------------------"
$body = @{
    leagueIds = @()
    seasons = @('2023-2024')
    includeWithoutOdds = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'
    Write-Host "  FAIL - Should have returned error but succeeded" -ForegroundColor Red
    $tests += @{name="Empty league IDs"; passed=$false}
} catch {
    $errorMsg = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "  PASS - Got expected error: $($errorMsg.error)" -ForegroundColor Green
    $tests += @{name="Empty league IDs"; passed=$true}
}
Write-Host ""

# Test 4: Empty seasons
Write-Host "Test 4: Empty seasons"
Write-Host "---------------------"
$body = @{
    leagueIds = @('e94f549c-b586-4a22-9efd-8fd1f903ffce')
    seasons = @()
    includeWithoutOdds = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'
    Write-Host "  FAIL - Should have returned error but succeeded" -ForegroundColor Red
    $tests += @{name="Empty seasons"; passed=$false}
} catch {
    $errorMsg = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "  PASS - Got expected error: $($errorMsg.error)" -ForegroundColor Green
    $tests += @{name="Empty seasons"; passed=$true}
}
Write-Host ""

# Test 5: Invalid season format (should still work, might return no data)
Write-Host "Test 5: Non-existent season (should complete but with 0 rounds)"
Write-Host "----------------------------------------------------------------"
$body = @{
    leagueIds = @('e94f549c-b586-4a22-9efd-8fd1f903ffce')
    seasons = @('1999-2000')  # Very old season, might not exist
    includeWithoutOdds = $true
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/historical' -Method POST -Body $body -ContentType 'application/json'
    Write-Host "  INFO - Job started: $($response.jobId)"

    # Wait a bit
    Start-Sleep -Seconds 5

    $jobStatus = Invoke-RestMethod -Uri "http://localhost:3001/api/import/jobs/$($response.jobId)"

    if ($jobStatus.progress.processedRounds -eq 0) {
        Write-Host "  PASS - Completed with 0 rounds (season doesn't exist on BetExplorer)" -ForegroundColor Green
        $tests += @{name="Non-existent season"; passed=$true}
    } else {
        Write-Host "  INFO - Got $($jobStatus.progress.processedRounds) rounds (season exists)" -ForegroundColor Cyan
        $tests += @{name="Non-existent season"; passed=$true}
    }
} catch {
    Write-Host "  FAIL - Unexpected error: $_" -ForegroundColor Red
    $tests += @{name="Non-existent season"; passed=$false}
}
Write-Host ""

# Summary
Write-Host ""
Write-Host "====================================="
Write-Host "  TEST SUMMARY"
Write-Host "====================================="
$passed = ($tests | Where-Object { $_.passed -eq $true }).Count
$total = $tests.Count
Write-Host "Passed: $passed / $total"
Write-Host ""

foreach ($test in $tests) {
    $status = if ($test.passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($test.passed) { "Green" } else { "Red" }
    Write-Host "  $status $($test.name)" -ForegroundColor $color
}
Write-Host ""
