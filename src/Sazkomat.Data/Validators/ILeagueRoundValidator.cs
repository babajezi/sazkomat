namespace Sazkomat.Data.Validators;

/// <summary>
/// Validator pro ověření, zda se liga hraje na kola (round-based) nebo je to pohár (cup)
/// </summary>
public interface ILeagueRoundValidator
{
    /// <summary>
    /// Ověří, zda se liga hraje na kola (round-based) nebo je to pohár
    /// </summary>
    /// <param name="leagueSlug">BetExplorer slug ligy (např. "premier-league")</param>
    /// <param name="countrySlug">BetExplorer slug země (např. "england")</param>
    /// <param name="season">Sezóna pro validaci (např. "2024-2025")</param>
    /// <param name="providerId">ID providera (BetExplorer)</param>
    /// <returns>true = round-based liga (chceme), false = pohár (ignorovat)</returns>
    Task<bool> IsRoundBasedLeagueAsync(
        string leagueSlug,
        string countrySlug,
        string season,
        Guid providerId
    );
}
