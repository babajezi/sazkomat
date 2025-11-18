#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fast Docker build with BuildKit

.DESCRIPTION
    Builds Docker images using BuildKit with cache mounts for maximum speed.
    Uses environment variable DOCKER_BUILDKIT=1.

.PARAMETER Service
    Service to build (default: api)

.PARAMETER NoCache
    Disable cache and perform clean build

.EXAMPLE
    ./build-fast.ps1
    ./build-fast.ps1 -Service frontend
    ./build-fast.ps1 -NoCache
#>

param(
    [string]$Service = "api",
    [switch]$NoCache
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Fast Docker Build - $Service" -ForegroundColor Cyan
Write-Host ""

# Enable BuildKit
$env:DOCKER_BUILDKIT = "1"
$env:COMPOSE_DOCKER_CLI_BUILD = "1"

Write-Host "BuildKit: ENABLED" -ForegroundColor Green
Write-Host "Service: $Service" -ForegroundColor Gray
Write-Host ""

$buildArgs = @("build", $Service)

if ($NoCache) {
    Write-Host "Cache: DISABLED (clean build)" -ForegroundColor Yellow
    $buildArgs += "--no-cache"
} else {
    Write-Host "Cache: ENABLED (incremental build)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Starting build..." -ForegroundColor Gray
Write-Host ""

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

try {
    & docker-compose @buildArgs
    $exitCode = $LASTEXITCODE
} catch {
    Write-Host "❌ Build failed: $_" -ForegroundColor Red
    exit 1
}

$stopwatch.Stop()
$elapsed = $stopwatch.Elapsed.TotalSeconds

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✅ Build completed in $([math]::Round($elapsed, 2))s" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed in $([math]::Round($elapsed, 2))s" -ForegroundColor Red
}

exit $exitCode
