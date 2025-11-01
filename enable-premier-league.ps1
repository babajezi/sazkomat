$body = @{
    isEnabled = $true
} | ConvertTo-Json

Write-Host "Enabling Premier League for import..."

$response = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/leagues/e94f549c-b586-4a22-9efd-8fd1f903ffce' -Method PATCH -Body $body -ContentType 'application/json'

Write-Host "League enabled!"
$response | ConvertTo-Json -Depth 5
