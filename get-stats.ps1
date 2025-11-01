$stats = Invoke-RestMethod -Uri 'http://localhost:3001/api/import/stats?leagueId=e94f549c-b586-4a22-9efd-8fd1f903ffce'

Write-Host "`nPremier League 2023-2024 Import Statistics:"
Write-Host "============================================"
Write-Host "Total Rounds: $($stats.totalRounds)"
Write-Host "Total Matches: $($stats.totalMatches)"
Write-Host "Home Wins: $($stats.totalHomeWins)"
Write-Host "Draws: $($stats.totalDraws)"
Write-Host "Away Wins: $($stats.totalAwayWins)"
Write-Host "With Complete Odds: $($stats.roundsWithCompleteOdds)"
Write-Host "Latest Scrape: $($stats.latestScrapedAt)"
Write-Host ""

$stats | ConvertTo-Json -Depth 5
