namespace Sazkomat.Configuration.Entities;

public enum ProviderType
{
    /// <summary>
    /// Data jsou získávána web scrapingem
    /// </summary>
    Scraper = 1,

    /// <summary>
    /// Data jsou získávána přes REST API
    /// </summary>
    API = 2,

    /// <summary>
    /// Data jsou vkládána manuálně
    /// </summary>
    Manual = 3,

    /// <summary>
    /// Sázková kancelář poskytující aktuální kurzy a seznam sázitelných lig
    /// </summary>
    BettingProvider = 4
}
