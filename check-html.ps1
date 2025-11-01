$html = Get-Content 'betexplorer-full.html' -Raw

Write-Host "HTML size: $($html.Length) chars" -ForegroundColor Yellow

$hasResultsContainer = $html -match 'js-leagueresults-all'
Write-Host "Contains 'js-leagueresults-all': $hasResultsContainer" -ForegroundColor $(if ($hasResultsContainer) { 'Green' } else { 'Red' })

$hasTableMain = $html -match 'table-main'
Write-Host "Contains 'table-main': $hasTableMain" -ForegroundColor $(if ($hasTableMain) { 'Green' } else { 'Red' })

$hasMatchLink = $html -match 'in-match'
Write-Host "Contains 'in-match' (match links): $hasMatchLink" -ForegroundColor $(if ($hasMatchLink) { 'Green' } else { 'Red' })

# Check for JavaScript-loaded content indicators
$hasReactRoot = $html -match 'react-root|data-reactroot|__NEXT_DATA__'
Write-Host "Contains React/Next.js markers: $hasReactRoot" -ForegroundColor Yellow

# Look for script tags
$scriptMatches = [regex]::Matches($html, '<script[^>]*src="([^"]*)"')
Write-Host "`nScript tags found: $($scriptMatches.Count)" -ForegroundColor Cyan
if ($scriptMatches.Count -gt 0) {
    Write-Host "Sample scripts:" -ForegroundColor Cyan
    $scriptMatches | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.Groups[1].Value)" -ForegroundColor White
    }
}
