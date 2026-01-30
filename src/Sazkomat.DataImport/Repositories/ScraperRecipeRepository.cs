using Microsoft.EntityFrameworkCore;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Entities;

namespace Sazkomat.DataImport.Repositories;

public class ScraperRecipeRepository : IScraperRecipeRepository
{
    private readonly DataImportDbContext _context;

    public ScraperRecipeRepository(DataImportDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScraperRecipe>> GetAllAsync()
    {
        return await _context.ScraperRecipes
            .OrderBy(r => r.Provider)
            .ThenBy(r => r.PageType)
            .ThenBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<ScraperRecipe?> GetByIdAsync(Guid id)
    {
        return await _context.ScraperRecipes.FindAsync(id);
    }

    public async Task<List<ScraperRecipe>> GetOrderedByPriorityAsync(string provider, string pageType)
    {
        return await _context.ScraperRecipes
            .Where(r => r.Provider == provider
                     && r.PageType == pageType
                     && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<List<ScraperRecipe>> GetByProviderAndPageTypeAsync(string provider, string pageType)
    {
        return await _context.ScraperRecipes
            .Where(r => r.Provider == provider && r.PageType == pageType)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<ScraperRecipe> CreateAsync(ScraperRecipe recipe)
    {
        _context.ScraperRecipes.Add(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task<ScraperRecipe> UpdateAsync(ScraperRecipe recipe)
    {
        _context.ScraperRecipes.Update(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task DeleteAsync(Guid id)
    {
        var recipe = await _context.ScraperRecipes.FindAsync(id);
        if (recipe != null)
        {
            _context.ScraperRecipes.Remove(recipe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementStatsAsync(Guid recipeId, bool success)
    {
        await _context.ScraperRecipes
            .Where(r => r.Id == recipeId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.TotalAttempts, r => r.TotalAttempts + 1)
                .SetProperty(r => r.SuccessfulAttempts, r => success ? r.SuccessfulAttempts + 1 : r.SuccessfulAttempts)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
    }

    public async Task<List<RecipeStats>> GetStatsAsync()
    {
        return await _context.ScraperRecipes
            .OrderBy(r => r.Provider)
            .ThenBy(r => r.PageType)
            .ThenByDescending(r => r.TotalAttempts)
            .Select(r => new RecipeStats
            {
                RecipeId = r.Id,
                Name = r.Name,
                Provider = r.Provider,
                PageType = r.PageType,
                TotalAttempts = r.TotalAttempts,
                SuccessfulAttempts = r.SuccessfulAttempts,
                SuccessRate = r.TotalAttempts > 0
                    ? (decimal)r.SuccessfulAttempts / r.TotalAttempts
                    : 0
            })
            .ToListAsync();
    }
}
