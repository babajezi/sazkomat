#!/bin/bash
# Sazkomat Development Startup Script
# Automatically kills processes on required ports and starts the application

echo "==============================================="
echo " Sazkomat Development Environment Startup"
echo "==============================================="
echo ""

# Define required ports
declare -A port_names=(
    [3000]="Frontend (Next.js)"
    [3001]="API (.NET)"
    [3002]="PostgreSQL"
    [3003]="Redis"
    [3004]="pgAdmin"
)

ports=(3000 3001 3002 3003 3004)

echo "Step 1: Checking and freeing ports..."
echo ""

for port in "${ports[@]}"; do
    service_name="${port_names[$port]}"
    echo -e "Checking port $port ($service_name)..."

    # Find process using the port
    if command -v lsof &> /dev/null; then
        # macOS/Linux with lsof
        pid=$(lsof -ti:$port 2>/dev/null)
    elif command -v netstat &> /dev/null; then
        # Linux with netstat
        pid=$(netstat -tulpn 2>/dev/null | grep ":$port " | awk '{print $7}' | cut -d'/' -f1)
    else
        echo "  Warning: Neither lsof nor netstat found, cannot check ports"
        continue
    fi

    if [ ! -z "$pid" ]; then
        process_name=$(ps -p $pid -o comm= 2>/dev/null || echo "unknown")
        echo "  Port $port is occupied by process: $process_name (PID: $pid)"
        echo "  Killing process..."
        kill -9 $pid 2>/dev/null
        sleep 0.5
        echo "  Process killed successfully"
    else
        echo "  Port $port is free"
    fi
done

echo ""
echo "Step 2: Starting Docker containers..."
echo ""

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo "ERROR: Docker is not running!"
    echo "Please start Docker and try again."
    exit 1
fi

# Start Docker Compose
echo "Starting services with docker-compose..."
docker-compose up -d

if [ $? -eq 0 ]; then
    echo "Docker containers started successfully!"
else
    echo "ERROR: Failed to start Docker containers!"
    echo "Check docker-compose.yml for errors."
    exit 1
fi

echo ""
echo "Step 3: Waiting for services to be ready..."
echo ""

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL..."
max_attempts=30
attempt=0
pg_ready=false

while [ $attempt -lt $max_attempts ] && [ "$pg_ready" = false ]; do
    if docker exec sazkomat-postgres pg_isready -U sazkomat >/dev/null 2>&1; then
        pg_ready=true
        echo "PostgreSQL is ready!"
    else
        echo "  Attempt $((attempt + 1))/$max_attempts..."
        sleep 1
        attempt=$((attempt + 1))
    fi
done

if [ "$pg_ready" = false ]; then
    echo "WARNING: PostgreSQL did not become ready in time"
fi

echo ""
echo "==============================================="
echo " Services Status"
echo "==============================================="
echo ""

docker-compose ps

echo ""
echo "==============================================="
echo " Access URLs"
echo "==============================================="
echo ""
echo "Frontend:   http://localhost:3000"
echo "API:        http://localhost:3001"
echo "Health:     http://localhost:3001/health"
echo "pgAdmin:    http://localhost:3004"
echo "            Email: admin@sazkomat.local"
echo "            Password: admin123"
echo ""
echo "Database:   localhost:3002"
echo "            User: sazkomat"
echo "            Password: sazkomat123"
echo "            Database: sazkomat_db"
echo ""
echo "==============================================="
