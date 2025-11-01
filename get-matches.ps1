$matches = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/matches?leagueId=e94f549c-b586-4a22-9efd-8fd1f903ffce&take=1'

Write-Host "`nMatch Statistics:"
Write-Host "==================="
Write-Host "Total Matches Imported: $($matches.totalCount)"
Write-Host ""

# Get a sample match
if ($matches.matches.Count -gt 0) {
    $match = $matches.matches[0]
    Write-Host "Sample Match:"
    Write-Host "  $($match.homeTeam) $($match.homeScore):$($match.awayScore) $($match.awayTeam)"
    Write-Host "  Result: $($match.result)"
    Write-Host "  Odds: H=$($match.homeOdds) D=$($match.drawOdds) A=$($match.awayOdds)"
    Write-Host "  Round: $($match.round.roundNumber)"
}
