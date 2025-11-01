# UserPromptSubmit Hook - Detects when user submits a prompt
# This runs when the user submits input to Claude Code

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

# Log the status change
$logEntry = "[$timestamp INF] Claude Code Status: Running | Hook: UserPromptSubmit | User submitted prompt"
Add-Content -Path $logFile -Value $logEntry

# Return success (no blocking)
exit 0
