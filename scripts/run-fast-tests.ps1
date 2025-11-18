#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs only fast unit tests (< 10s target)

.DESCRIPTION
    Executes tests categorized as "Fast" (unit and repository tests).
    Uses parallel execution for maximum speed.

.PARAMETER Verbose
    Show detailed test output

.EXAMPLE
    ./run-fast-tests.ps1
    ./run-fast-tests.ps1 -Verbose
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Running Fast Tests (Unit + Repository)" -ForegroundColor Cyan
Write-Host "Expected runtime: < 10 seconds" -ForegroundColor Gray
Write-Host ""

$testProject = "tests/Sazkomat.Tests/Sazkomat.Tests.csproj"
$filter = "Category=Fast"

if ($Verbose) {
    $verbosity = "normal"
} else {
    $verbosity = "minimal"
}

Write-Host "Filter: $filter" -ForegroundColor Gray
Write-Host ""

# Measure execution time
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
    Write-Host "✅ Fast tests passed in $([math]::Round($elapsed, 2))s" -ForegroundColor Green

    if ($elapsed -gt 10) {
        Write-Host "⚠️  Warning: Tests took longer than expected (> 10s)" -ForegroundColor Yellow
    }
} else {
    Write-Host "❌ Fast tests failed in $([math]::Round($elapsed, 2))s" -ForegroundColor Red
}

exit $exitCode
