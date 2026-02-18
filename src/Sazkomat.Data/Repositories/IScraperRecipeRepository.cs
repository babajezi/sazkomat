using Sazkomat.Data.Entities;

namespace Sazkomat.Data.Repositories;

public interface IScraperRecipeRepository
{
    Task<List<ScraperRecipe>> GetAllAsync();

    Task<ScraperRecipe?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all active recipes for a provider and page type, ordered by priority
    /// </summary>
    Task<List<ScraperRecipe>> GetOrderedByPriorityAsync(string provider, string pageType);

    /// <summary>
    /// Gets all recipes (including inactive) for a provider and page type
    /// </summary>
    Task<List<ScraperRecipe>> GetByProviderAndPageTypeAsync(string provider, string pageType);

    Task<ScraperRecipe> CreateAsync(ScraperRecipe recipe);

    Task<ScraperRecipe> UpdateAsync(ScraperRecipe recipe);

    Task DeleteAsync(Guid id);

    /// <summary>
    /// Increments the attempt statistics for a recipe
    /// </summary>
    Task IncrementStatsAsync(Guid recipeId, bool success);

    /// <summary>
    /// Gets recipe statistics summary grouped by provider
    /// </summary>
    Task<List<RecipeStats>> GetStatsAsync();
}

/// <summary>
/// DTO for recipe statistics
/// </summary>
public class RecipeStats
{
    public Guid RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string PageType { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public decimal SuccessRate { get; set; }
}
