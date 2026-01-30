namespace Sazkomat.Configuration.Entities;

/// <summary>
/// Reason why a LeagueSeason has no data (HasData = false)
/// </summary>
public enum NoDataReason
{
    /// <summary>
    /// Data was loaded successfully or sync not yet attempted
    /// </summary>
    None = 0,

    /// <summary>
    /// BetExplorer page not found (301/404 redirect)
    /// Display: "Stránka neexistuje"
    /// </summary>
    PageNotFound = 1,

    /// <summary>
    /// Scraper found no rounds on the page
    /// Display: "Žádná kola"
    /// </summary>
    NoRoundsFound = 2,

    /// <summary>
    /// HTML parsing error
    /// Display: "Chyba parsování"
    /// </summary>
    ParsingError = 3,

    /// <summary>
    /// Network/timeout error
    /// Display: "Síťová chyba"
    /// </summary>
    NetworkError = 4,

    /// <summary>
    /// Data is partial - some rounds/matches are missing or cancelled
    /// Display: "Částečná data"
    /// Note field contains explanation (e.g., "Sezóna zrušena po 1. kole")
    /// </summary>
    PartialData = 5
}
