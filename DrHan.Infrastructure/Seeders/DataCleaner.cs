using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Seeders
{
    public static class DataCleaner
    {
        public static async Task CleanAllAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting data cleaning process...");

                // Clean in reverse order of dependencies (opposite of seeding order)
                // 7. IngredientAllergens (depends on Ingredients and Allergens) - clean first
                await CleanEntityAsync<IngredientAllergen>(context, logger);

                // 6. IngredientNames (depends on Ingredients)
                await CleanEntityAsync<IngredientName>(context, logger);

                // 5. Ingredients (no dependencies on others we're cleaning)
                await CleanEntityAsync<Ingredient>(context, logger);

                // 4. AllergenCrossReactivities (depends on Allergens and CrossReactivityGroups)
                await CleanEntityAsync<AllergenCrossReactivity>(context, logger);

                // 3. AllergenNames (depends on Allergens)
                await CleanEntityAsync<AllergenName>(context, logger);

                // 2. Allergens (no dependencies on others we're cleaning)
                await CleanEntityAsync<Allergen>(context, logger);

                // 1. CrossReactivityGroups (no dependencies) - clean last
                await CleanEntityAsync<CrossReactivityGroup>(context, logger);

                logger?.LogInformation("Data cleaning completed successfully!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during data cleaning");
                throw;
            }
        }

        public static async Task CleanAllergenDataAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting allergen data cleaning...");

                // Clean allergen-related data in reverse dependency order
                await CleanEntityAsync<IngredientAllergen>(context, logger); // Remove ingredient-allergen links first
                await CleanEntityAsync<AllergenCrossReactivity>(context, logger);
                await CleanEntityAsync<AllergenName>(context, logger);
                await CleanEntityAsync<Allergen>(context, logger);
                await CleanEntityAsync<CrossReactivityGroup>(context, logger);

                logger?.LogInformation("Allergen data cleaning completed!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during allergen data cleaning");
                throw;
            }
        }

        public static async Task CleanIngredientDataAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting ingredient data cleaning...");

                // Clean ingredient-related data in reverse dependency order
                await CleanEntityAsync<IngredientAllergen>(context, logger);
                await CleanEntityAsync<IngredientName>(context, logger);
                await CleanEntityAsync<Ingredient>(context, logger);

                logger?.LogInformation("Ingredient data cleaning completed!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during ingredient data cleaning");
                throw;
            }
        }

        public static async Task CleanEntityAsync<T>(ApplicationDbContext context, ILogger? logger = null) where T : class
        {
            try
            {
                var entityName = typeof(T).Name;
                logger?.LogInformation($"Cleaning {entityName} data...");

                var entities = await context.Set<T>().ToListAsync();
                if (entities.Any())
                {
                    context.Set<T>().RemoveRange(entities);
                    await context.SaveChangesAsync();
                    logger?.LogInformation($"Successfully cleaned {entities.Count} {entityName} records.");
                }
                else
                {
                    logger?.LogInformation($"No {entityName} data found to clean.");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Error occurred while cleaning {typeof(T).Name}");
                throw;
            }
        }

        public static async Task ResetAndReseedAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting reset and reseed process...");

                // First clean all data
                await CleanAllAsync(context, logger);

                // Then seed fresh data
                await MasterSeeder.SeedAllAsync(context, logger);

                logger?.LogInformation("Reset and reseed completed successfully!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during reset and reseed");
                throw;
            }
        }

        public static async Task<DataStatistics> GetDataStatisticsAsync(ApplicationDbContext context)
        {
            return new DataStatistics
            {
                CrossReactivityGroupsCount = await context.CrossReactivityGroups.CountAsync(),
                AllergensCount = await context.Allergens.CountAsync(),
                AllergenNamesCount = await context.AllergenNames.CountAsync(),
                AllergenCrossReactivitiesCount = await context.AllergenCrossReactivities.CountAsync(),
                IngredientsCount = await context.Ingredients.CountAsync(),
                IngredientNamesCount = await context.IngredientNames.CountAsync(),
                IngredientAllergensCount = await context.IngredientAllergens.CountAsync()
            };
        }

        public class DataStatistics
        {
            public int CrossReactivityGroupsCount { get; set; }
            public int AllergensCount { get; set; }
            public int AllergenNamesCount { get; set; }
            public int AllergenCrossReactivitiesCount { get; set; }
            public int IngredientsCount { get; set; }
            public int IngredientNamesCount { get; set; }
            public int IngredientAllergensCount { get; set; }

            public int TotalRecords => CrossReactivityGroupsCount + AllergensCount + AllergenNamesCount + 
                                     AllergenCrossReactivitiesCount + IngredientsCount + IngredientNamesCount + 
                                     IngredientAllergensCount;

            public override string ToString()
            {
                return $@"
=== Database Statistics ===
CrossReactivityGroups: {CrossReactivityGroupsCount}
Allergens: {AllergensCount}
AllergenNames: {AllergenNamesCount}
AllergenCrossReactivities: {AllergenCrossReactivitiesCount}
Ingredients: {IngredientsCount}
IngredientNames: {IngredientNamesCount}
IngredientAllergens: {IngredientAllergensCount}
────────────────────────────
Total Records: {TotalRecords}";
            }
        }
    }
} 