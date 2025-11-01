namespace Sazkomat.DataImport.Scrapers;

public interface IHttpClient
{
    Task<string> GetHtmlAsync(string url);
}
