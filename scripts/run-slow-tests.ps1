#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs slow and integration tests

.DESCRIPTION
    Executes tests categorized as "Slow" or "Integration".
    These include service tests, scrapers, and orchestrators.

.PARAMETER Verbose
    Show detailed test output

.EXAMPLE
    ./run-slow-tests.ps1
    ./run-slow-tests.ps1 -Verbose
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "🐌 Running Slow + Integration Tests" -ForegroundColor Cyan
Write-Host "Expected runtime: < 60 seconds" -ForegroundColor Gray
Write-Host ""

$testProject = "tests/Sazkomat.Tests/Sazkomat.Tests.csproj"
$filter = "Category=Slow|Category=Integration"

if ($Verbose) {
    $verbosity = "normal"
} else {
    $verbosity = "minimal"
}

Write-Host "Filter: $filter" -ForegroundColor Gray
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    dotnet test $testProject `
        --filter $filter `
        --verbosity $verbosity `
        --nologo `
        --no-build `
        --configuration Debug

    $exitCode = $LASTEXITCODE
} catch {
    Write-Host "❌ Test execution failed: $_" -ForegroundColor Red
    exit 1
}

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed.TotalSeconds

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✅ Slow tests passed in $([math]::Round($elapsed, 2))s" -ForegroundColor Green

    if ($elapsed -gt 60) {
        Write-Host "⚠️  Warning: Tests took longer than expected (> 60s)" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Slow tests failed in $([math]::Round($elapsed, 2))s" -ForegroundColor Red
}

exit $exitCode
