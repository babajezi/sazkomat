$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'
$continentCodes = @('europe', 'africa', 'asia', 'australia-oceania', 'oceania', 'north-central-america', 'south-america')

foreach ($country in $countries) {
    if ($continentCodes -contains $country.code) {
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
