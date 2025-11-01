$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'

Write-Host "Deleting $($countries.Count) countries..."

foreach ($country in $countries) {
    Write-Host "Deleting: $($country.name) ($($country.code))"
    try {
        Invoke-RestMethod -Uri "http://localhost:3001/api/config/countries/$($country.id)" -Method DELETE
    } catch {
        Write-Host "Error deleting $($country.name): $_"
    }
}

Write-Host "All countries deleted!"
