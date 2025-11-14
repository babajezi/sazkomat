#!/bin/bash

# Run .NET tests with code coverage
# Generates both Cobertura (XML) and HTML reports

set -e

echo "🧪 Running tests with code coverage..."

# Navigate to test project
cd "$(dirname "$0")/../tests/Sazkomat.Tests"

# Clean previous coverage results
rm -rf TestResults
rm -rf CoverageReport

# Run tests with coverage collection
dotnet test \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=./TestResults/coverage.cobertura.xml \
  /p:ExcludeByFile="**/Migrations/**/*.cs" \
  --verbosity normal

echo ""
echo "✅ Tests completed with coverage data collected"
echo ""
echo "📊 Coverage file: tests/Sazkomat.Tests/TestResults/coverage.cobertura.xml"
echo ""

# Generate HTML report if reportgenerator is available
if command -v reportgenerator &> /dev/null; then
    echo "📈 Generating HTML coverage report..."
    reportgenerator \
      -reports:./TestResults/coverage.cobertura.xml \
      -targetdir:./CoverageReport \
      -reporttypes:Html

    echo ""
    echo "✅ HTML report generated: tests/Sazkomat.Tests/CoverageReport/index.html"
    echo ""
else
    echo "ℹ️  Install reportgenerator to generate HTML reports:"
    echo "   dotnet tool install -g dotnet-reportgenerator-globaltool"
    echo ""
fi

echo "🎉 Done!"
