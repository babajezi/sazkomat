using Sazkomat.Configuration.Entities;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Scrapers;

public interface ILeagueScraper
{
    Task<List<Round>> ScrapeSeasonAsync(League league, string season);
    bool CanHandle(Sport sport);
}
