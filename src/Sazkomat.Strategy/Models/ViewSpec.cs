using System.Text.Json.Serialization;

namespace Sazkomat.Strategy.Models;

public class ViewSpec
{
    [JsonPropertyName("dataSource")]
    public string DataSource { get; set; } = "matches";

    [JsonPropertyName("filters")]
    public ViewFilters? Filters { get; set; }

    [JsonPropertyName("groupBy")]
    public List<string>? GroupBy { get; set; }

    [JsonPropertyName("metrics")]
    public List<MetricSpec> Metrics { get; set; } = new();

    [JsonPropertyName("sort")]
    public SortSpec? Sort { get; set; }

    [JsonPropertyName("limit")]
    public int? Limit { get; set; }

    [JsonPropertyName("visualization")]
    public VisualizationSpec? Visualization { get; set; }
}

public class ViewFilters
{
    [JsonPropertyName("leagueIds")]
    public List<Guid>? LeagueIds { get; set; }

    [JsonPropertyName("countryIds")]
    public List<Guid>? CountryIds { get; set; }

    [JsonPropertyName("seasonNames")]
    public List<string>? SeasonNames { get; set; }

    [JsonPropertyName("dateRange")]
    public DateRangeFilter? DateRange { get; set; }

    [JsonPropertyName("results")]
    public List<string>? Results { get; set; }

    [JsonPropertyName("hasOdds")]
    public bool? HasOdds { get; set; }

    [JsonPropertyName("oddsRange")]
    public OddsRangeFilter? OddsRange { get; set; }

    [JsonPropertyName("minMatches")]
    public int? MinMatches { get; set; }

    [JsonPropertyName("fieldComparisons")]
    public List<FieldComparison>? FieldComparisons { get; set; }
}

public class DateRangeFilter
{
    [JsonPropertyName("from")]
    public DateTime? From { get; set; }

    [JsonPropertyName("to")]
    public DateTime? To { get; set; }
}

public class OddsRangeFilter
{
    [JsonPropertyName("column")]
    public string Column { get; set; } = "home_odds";

    [JsonPropertyName("min")]
    public decimal? Min { get; set; }

    [JsonPropertyName("max")]
    public decimal? Max { get; set; }
}

public class FieldComparison
{
    [JsonPropertyName("left")]
    public string Left { get; set; } = string.Empty;

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "=";

    [JsonPropertyName("right")]
    public string Right { get; set; } = string.Empty;
}

public class MetricSpec
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "count";

    [JsonPropertyName("column")]
    public string? Column { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}

public class SortSpec
{
    [JsonPropertyName("column")]
    public string Column { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "desc";
}

public class VisualizationSpec
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "table";

    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; }
}
