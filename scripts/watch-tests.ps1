#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Continuous test execution (watch mode)

.DESCRIPTION
    Watches for file changes and automatically re-runs fast tests.
    Perfect for TDD workflow.

.PARAMETER Filter
    Test filter (default: Category=Fast)

.EXAMPLE
    ./watch-tests.ps1
    ./watch-tests.ps1 -Filter "Type=Repository"
#>

param(
    [string]$Filter = "Category=Fast"
)

$ErrorActionPreference = "Stop"

Write-Host "👀 Watch Mode - Continuous Test Execution" -ForegroundColor Cyan
Write-Host "Filter: $Filter" -ForegroundColor Gray
Write-Host "Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

$testProject = "tests/Sazkomat.Tests/Sazkomat.Tests.csproj"

dotnet watch test $testProject `
    --filter $Filter `
    --verbosity minimal `
    --nologo `
    --configuration Debug
