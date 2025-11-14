namespace Sazkomat.DataImport.Entities;

/// <summary>
/// Status of provider league mapping to BetExplorer
/// </summary>
public enum MappingStatus
{
    /// <summary>
    /// League has not been mapped to BetExplorer (ProviderSlug is empty or null)
    /// </summary>
    Unmapped = 0,

    /// <summary>
    /// League was automatically mapped via enrichment service (fuzzy matching)
    /// </summary>
    AutoMapped = 1,

    /// <summary>
    /// League was manually mapped via LeagueNameMapping table
    /// </summary>
    ManualMapped = 2
}
