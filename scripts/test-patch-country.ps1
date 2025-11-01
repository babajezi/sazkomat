$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'
$firstCountry = $countries[0]

Write-Host "First country: $($firstCountry.name) (ID: $($firstCountry.id))"
Write-Host "Current isActive: $($firstCountry.isActive)"
Write-Host ""
Write-Host "Attempting to PATCH country to set isActive=true..."

try {
    $body = @{ isActive = $true } | ConvertTo-Json
    $result = Invoke-RestMethod -Uri "http://localhost:3001/api/config/countries/$($firstCountry.id)" `
        -Method PATCH `
        -Body $body `
        -ContentType "application/json"

    Write-Host "SUCCESS! Country updated:" -ForegroundColor Green
    $result | ConvertTo-Json

    Write-Host ""
    Write-Host "Verifying with GET..."
    $updated = Invoke-RestMethod -Uri "http://localhost:3001/api/config/countries/$($firstCountry.id)"
    Write-Host "isActive is now: $($updated.isActive)"
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message
}
