# Enable all top European leagues

$leagues = @(
    @{name="La Liga"; id="f22c1a9c-4528-4d20-aa26-c139d3d2afc1"},
    @{name="Bundesliga"; id="5b040f3a-95b6-472a-ab7d-235c0e572183"},
    @{name="Serie A"; id="2b6666dd-7dc8-4ef8-88ba-46d03d847aab"},
    @{name="Ligue 1"; id="edaec3ad-c419-420c-aae3-5e175893957e"}
)

$body = @{isEnabled = $true} | ConvertTo-Json

Write-Host ""
Write-Host "Enabling all top European leagues..."
Write-Host "======================================"

foreach ($league in $leagues) {
    Write-Host ""
    Write-Host "Enabling $($league.name)..."
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:3001/api/config/leagues/$($league.id)" -Method PATCH -Body $body -ContentType 'application/json'
        Write-Host "  OK - $($league.name) enabled!" -ForegroundColor Green
    } catch {
        Write-Host "  ERROR - Failed to enable $($league.name): $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "All leagues enabled!"
