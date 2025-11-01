namespace Sazkomat.DataImport.Entities;

public class ImportProgressData
{
    public Guid? CurrentSeasonId { get; set; }
    public List<Guid> ProcessedSeasonIds { get; set; } = new();
    public int TotalSeasons { get; set; }
    public int ProcessedRounds { get; set; }
    public List<string> Errors { get; set; } = new();
}
