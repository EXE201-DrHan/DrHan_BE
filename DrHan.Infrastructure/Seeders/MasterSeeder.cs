using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Seeders
{
    public static class MasterSeeder
    {
        public static async Task SeedAllAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting data seeding process...");

                // Seed in order of dependencies using generic seeder with extension methods
                // 1. CrossReactivityGroups (no dependencies)
                await context.SeedFromJsonAsync<CrossReactivityGroup>(
                    SeederConfiguration.FilePaths.CrossReactivityGroups, 
                    SeederConfiguration.JsonParsers.ParseCrossReactivityGroups, logger);

                // 2. Allergens (no dependencies)
                await context.SeedFromJsonAsync<Allergen>(
                    SeederConfiguration.FilePaths.Allergens, 
                    SeederConfiguration.JsonParsers.ParseAllergens, logger);

                // 3. AllergenNames (depends on Allergens)
                await context.SeedFromJsonAsync<AllergenName>(
                    SeederConfiguration.FilePaths.AllergenNames, 
                    SeederConfiguration.JsonParsers.ParseAllergenNames, logger);

                // 4. AllergenCrossReactivities (depends on Allergens and CrossReactivityGroups)
                await context.SeedFromJsonAsync<AllergenCrossReactivity>(
                    SeederConfiguration.FilePaths.AllergenCrossReactivities, 
                    SeederConfiguration.JsonParsers.ParseAllergenCrossReactivities, logger);

                // 5. Ingredients (no dependencies)
                await context.SeedFromJsonAsync<Ingredient>(
                    SeederConfiguration.FilePaths.Ingredients, 
                    SeederConfiguration.JsonParsers.ParseIngredients, logger);

                // 6. IngredientNames (depends on Ingredients)
                await context.SeedFromJsonAsync<IngredientName>(
                    SeederConfiguration.FilePaths.IngredientNames, 
                    SeederConfiguration.JsonParsers.ParseIngredientNames, logger);

                // 7. IngredientAllergens (depends on Ingredients and Allergens)
                await context.SeedFromJsonAsync<IngredientAllergen>(
                    SeederConfiguration.FilePaths.IngredientAllergens, 
                    SeederConfiguration.JsonParsers.ParseIngredientAllergens, logger);
                logger?.LogInformation("Data seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during data seeding");
                throw;
            }
        }

        public static async Task SeedAllergenDataAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting allergen data seeding...");

                // Seed allergen-related data only using extension methods
                await context.SeedFromJsonAsync<CrossReactivityGroup>(
                    SeederConfiguration.FilePaths.CrossReactivityGroups, 
                    SeederConfiguration.JsonParsers.ParseCrossReactivityGroups, logger);

                await context.SeedFromJsonAsync<Allergen>(
                    SeederConfiguration.FilePaths.Allergens, 
                    SeederConfiguration.JsonParsers.ParseAllergens, logger);

                await context.SeedFromJsonAsync<AllergenName>(
                    SeederConfiguration.FilePaths.AllergenNames, 
                    SeederConfiguration.JsonParsers.ParseAllergenNames, logger);

                await context.SeedFromJsonAsync<AllergenCrossReactivity>(
                    SeederConfiguration.FilePaths.AllergenCrossReactivities, 
                    SeederConfiguration.JsonParsers.ParseAllergenCrossReactivities, logger);

                logger?.LogInformation("Allergen data seeding completed!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during allergen data seeding");
                throw;
            }
        }

        public static async Task SeedIngredientDataAsync(ApplicationDbContext context, ILogger? logger = null)
        {
            try
            {
                logger?.LogInformation("Starting ingredient data seeding...");

                // Seed ingredient-related data only using extension methods
                await context.SeedFromJsonAsync<Ingredient>(
                    SeederConfiguration.FilePaths.Ingredients, 
                    SeederConfiguration.JsonParsers.ParseIngredients, logger);

                await context.SeedFromJsonAsync<IngredientName>(
                    SeederConfiguration.FilePaths.IngredientNames, 
                    SeederConfiguration.JsonParsers.ParseIngredientNames, logger);

                await context.SeedFromJsonAsync<IngredientAllergen>(
                    SeederConfiguration.FilePaths.IngredientAllergens, 
                    SeederConfiguration.JsonParsers.ParseIngredientAllergens, logger);

                logger?.LogInformation("Ingredient data seeding completed!");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error occurred during ingredient data seeding");
                throw;
            }
        }
    }
} 