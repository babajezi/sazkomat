# Stop Hook - Detects when Claude Code finishes responding
# This runs when Claude has finished processing and is idle

$projectRoot = "C:\projects\private\Sazkomat"
$logDir = Join-Path $projectRoot "logs"
$logFile = Join-Path $logDir ("claude-code-sazkomat-" + (Get-Date -Format "yyyyMMdd") + ".log")

# Ensure log directory exists
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

# Consume stdin without blocking (hooks receive JSON but we don't need to parse it)
$null = $input

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff zzz"

# Log the event (without changing status - stop doesn't mean idle, could still need input)
$logEntry = "[$timestamp DBG] Hook: Stop | Finished responding"
Add-Content -Path $logFile -Value $logEntry

# Return success (no blocking)
exit 0
