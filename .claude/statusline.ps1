# Sazkomat Script for Monitoring
# This script logs Claude Code activity to logs/claude-code-sazkomat-{date}.log

$projectRoot = "C:\projects\private\Sazkomat"
$logDir = Join-Path $projectRoot "logs"
$logFile = Join-Path $logDir ("claude-code-sazkomat-" + (Get-Date -Format "yyyyMMdd") + ".log")

# Ensure log directory exists
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir -Force | Out-Null
}

# Get environment variables from Claude Code
$conversationId = $env:CONVERSATION_ID
$needsInput = $env:NEEDS_INPUT
$runStatus = $env:RUN_STATUS
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff zzz"

# Determine status
$status = "Idle"
if ($needsInput -eq "true") {
    $status = "NeedsInput"
    $logLevel = "WRN"
} elseif ($runStatus -eq "running") {
    $status = "Running"
    $logLevel = "INF"
} else {
    $logLevel = "INF"
}

# Log entry
$logEntry = "[$timestamp $logLevel] Claude Code Status: $status | ConversationId: $conversationId | NeedsInput: $needsInput | RunStatus: $runStatus"
Add-Content -Path $logFile -Value $logEntry

# Output for statusline (optional - what shows in terminal)
Write-Output "Sazkomat | $status"
