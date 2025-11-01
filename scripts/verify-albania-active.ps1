$countries = Invoke-RestMethod -Uri 'http://localhost:3001/api/config/countries'
$albania = $countries | Where-Object { $_.name -eq 'Albania' }

if ($albania) {
    Write-Host "Albania found:"
    Write-Host "  ID: $($albania.id)"
    Write-Host "  isActive: $($albania.isActive)"
} else {
    Write-Host "Albania not found!"
}
