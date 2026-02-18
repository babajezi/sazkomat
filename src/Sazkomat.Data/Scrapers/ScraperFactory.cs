using Sazkomat.Configuration.Entities;

namespace Sazkomat.Data.Scrapers;

public class ScraperFactory
{
    private readonly IEnumerable<ILeagueScraper> _scrapers;

    public ScraperFactory(IEnumerable<ILeagueScraper> scrapers)
    {
        _scrapers = scrapers;
    }

    public ILeagueScraper GetScraper(Sport sport)
    {
        var scraper = _scrapers.FirstOrDefault(s => s.CanHandle(sport));

        if (scraper == null)
        {
            throw new NotSupportedException($"No scraper available for sport: {sport.Name}");
        }

        return scraper;
    }
}
