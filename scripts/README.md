# Scripts

Utility scripts for development and testing.

## Code Coverage

### run-tests-with-coverage.sh (Linux/macOS)
```bash
chmod +x scripts/run-tests-with-coverage.sh
./scripts/run-tests-with-coverage.sh
```

### run-tests-with-coverage.ps1 (Windows)
```powershell
powershell.exe -ExecutionPolicy Bypass -File scripts/run-tests-with-coverage.ps1
```

### Output
- **Cobertura XML**: `tests/Sazkomat.Tests/TestResults/coverage.cobertura.xml`
- **HTML Report**: `tests/Sazkomat.Tests/CoverageReport/index.html` (if reportgenerator is installed)

### Install ReportGenerator (Optional)
For HTML coverage reports, install the global tool:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

## Other Scripts

Add other utility scripts here as the project grows.
