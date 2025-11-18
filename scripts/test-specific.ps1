#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run tests from a specific test class

.DESCRIPTION
    Helper script to run all tests from a specific test class.

.PARAMETER ClassName
    Test class name (e.g., LeagueRepositoryTests, ScanServiceTests)

.PARAMETER Verbose
    Show detailed test output

.EXAMPLE
    ./test-specific.ps1 -ClassName LeagueRepositoryTests
    ./test-specific.ps1 -ClassName ScanServiceTests -Verbose
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ClassName,

    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

Write-Host "🎯 Running tests from: $ClassName" -ForegroundColor Cyan
Write-Host ""

$testProject = "tests/Sazkomat.Tests/Sazkomat.Tests.csproj"
$filter = "FullyQualifiedName~$ClassName"

if ($Verbose) {
    $verbosity = "normal"
} else {
    $verbosity = "minimal"
}

Write-Host "Filter: $filter" -ForegroundColor Gray
Write-Host ""

dotnet test $testProject `
    --filter $filter `
    --verbosity $verbosity `
    --nologo `
    --configuration Debug
