# Sazkomat Development Startup Script
# Automatically kills processes on required ports and starts the application

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Sazkomat Development Environment Startup" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Define required ports
$ports = @(3000, 3001, 3002, 3003, 3004)
$portNames = @{
    3000 = "Frontend (Next.js)"
    3001 = "API (.NET)"
    3002 = "PostgreSQL"
    3003 = "Redis"
    3004 = "pgAdmin"
}

Write-Host "Step 1: Checking and freeing ports..." -ForegroundColor Yellow
Write-Host ""

foreach ($port in $ports) {
    $serviceName = $portNames[$port]
    Write-Host "Checking port $port ($serviceName)..." -ForegroundColor Gray

    try {
        $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue

        if ($connection) {
            $processId = $connection.OwningProcess | Select-Object -First 1
            $process = Get-Process -Id $processId -ErrorAction SilentlyContinue

            if ($process) {
                Write-Host "  Port $port is occupied by process: $($process.ProcessName) (PID: $processId)" -ForegroundColor Red
                Write-Host "  Killing process..." -ForegroundColor Yellow
                Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
                Start-Sleep -Milliseconds 500
                Write-Host "  Process killed successfully" -ForegroundColor Green
            }
        } else {
            Write-Host "  Port $port is free" -ForegroundColor Green
        }
    } catch {
        Write-Host "  Port $port is free" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Step 2: Starting Docker containers..." -ForegroundColor Yellow
Write-Host ""

# Check if Docker Desktop is running
try {
    $dockerInfo = docker info 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Docker Desktop is not running!" -ForegroundColor Red
        Write-Host "Please start Docker Desktop and try again." -ForegroundColor Yellow
        pause
        exit 1
    }
} catch {
    Write-Host "ERROR: Docker is not installed or not running!" -ForegroundColor Red
    Write-Host "Please install Docker Desktop and try again." -ForegroundColor Yellow
    pause
    exit 1
}

# Start Docker Compose
Write-Host "Starting services with docker-compose..." -ForegroundColor Gray
docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker containers started successfully!" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to start Docker containers!" -ForegroundColor Red
    Write-Host "Check docker-compose.yml for errors." -ForegroundColor Yellow
    pause
    exit 1
}

Write-Host ""
Write-Host "Step 3: Waiting for services to be ready..." -ForegroundColor Yellow
Write-Host ""

# Wait for PostgreSQL to be ready
Write-Host "Waiting for PostgreSQL..." -ForegroundColor Gray
$maxAttempts = 30
$attempt = 0
$pgReady = $false

while ($attempt -lt $maxAttempts -and -not $pgReady) {
    try {
        $testConnection = docker exec sazkomat-postgres pg_isready -U sazkomat 2>&1
        if ($testConnection -like "*accepting connections*") {
            $pgReady = $true
            Write-Host "PostgreSQL is ready!" -ForegroundColor Green
        } else {
            Write-Host "  Attempt $($attempt + 1)/$maxAttempts..." -ForegroundColor Gray
            Start-Sleep -Seconds 1
            $attempt++
        }
    } catch {
        Write-Host "  Attempt $($attempt + 1)/$maxAttempts..." -ForegroundColor Gray
        Start-Sleep -Seconds 1
        $attempt++
    }
}

if (-not $pgReady) {
    Write-Host "WARNING: PostgreSQL did not become ready in time" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Services Status" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

docker-compose ps

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " Access URLs" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Frontend:   http://localhost:3000" -ForegroundColor Green
Write-Host "API:        http://localhost:3001" -ForegroundColor Green
Write-Host "Health:     http://localhost:3001/health" -ForegroundColor Green
Write-Host "pgAdmin:    http://localhost:3004" -ForegroundColor Green
Write-Host "            Email: admin@sazkomat.local" -ForegroundColor Gray
Write-Host "            Password: admin123" -ForegroundColor Gray
Write-Host ""
Write-Host "Database:   localhost:3002" -ForegroundColor Green
Write-Host "            User: sazkomat" -ForegroundColor Gray
Write-Host "            Password: sazkomat123" -ForegroundColor Gray
Write-Host "            Database: sazkomat_db" -ForegroundColor Gray
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Press any key to exit..." -ForegroundColor Gray
pause
