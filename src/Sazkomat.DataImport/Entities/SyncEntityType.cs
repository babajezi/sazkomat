namespace Sazkomat.DataImport.Entities;

public enum SyncEntityType
{
    Countries,
    Leagues,
    Seasons,
    Rounds,
    /// <summary>
    /// Combined scan of countries AND leagues in single pass.
    /// Optimized for betting providers like Betano where both come from one HTTP request.
    /// </summary>
    CountriesAndLeagues
}
