# Run .NET tests with code coverage
# Generates both Cobertura (XML) and HTML reports

$ErrorActionPreference = "Stop"

Write-Host "🧪 Running tests with code coverage..." -ForegroundColor Cyan

# Navigate to test project
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$testProjectPath = Join-Path $scriptPath "..\tests\Sazkomat.Tests"
Set-Location $testProjectPath

# Clean previous coverage results
if (Test-Path "TestResults") {
    Remove-Item -Recurse -Force "TestResults"
}
if (Test-Path "CoverageReport") {
    Remove-Item -Recurse -Force "CoverageReport"
}

# Run tests with coverage collection
dotnet test `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:CoverletOutput=./TestResults/coverage.cobertura.xml `
  /p:ExcludeByFile="**/Migrations/**/*.cs" `
  --verbosity normal

Write-Host ""
Write-Host "✅ Tests completed with coverage data collected" -ForegroundColor Green
Write-Host ""
Write-Host "📊 Coverage file: tests/Sazkomat.Tests/TestResults/coverage.cobertura.xml" -ForegroundColor Yellow
Write-Host ""

# Generate HTML report if reportgenerator is available
if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
    Write-Host "📈 Generating HTML coverage report..." -ForegroundColor Cyan

    reportgenerator `
      -reports:./TestResults/coverage.cobertura.xml `
      -targetdir:./CoverageReport `
      -reporttypes:Html

    Write-Host ""
    Write-Host "✅ HTML report generated: tests/Sazkomat.Tests/CoverageReport/index.html" -ForegroundColor Green
    Write-Host ""

    # Try to open the report in default browser
    $reportPath = Join-Path (Get-Location) "CoverageReport\index.html"
    if (Test-Path $reportPath) {
        Write-Host "🌐 Opening report in browser..." -ForegroundColor Cyan
        Start-Process $reportPath
    }
} else {
    Write-Host "ℹ️  Install reportgenerator to generate HTML reports:" -ForegroundColor Yellow
    Write-Host "   dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "🎉 Done!" -ForegroundColor Green
