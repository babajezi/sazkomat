$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'
$invalidCodes = @('popular-bets', 'odds-movements', 'odds-filter', 'livescore')

foreach ($country in $countries) {
    if ($invalidCodes -contains $country.code) {
        Write-Host "Deleting: $($country.name) ($($country.code))"
        try {
            Invoke-RestMethod -Uri "http://localhost:3001/api/config/countries/$($country.id)" -Method DELETE
            Write-Host "Deleted successfully"
        } catch {
            Write-Host "Error: $_"
        }
    }
}

Write-Host "Cleanup completed!"
