using System.Reflection;
using System.Text.Json;

namespace DrHan.Infrastructure.Seeders
{
    public static class DataValidationHelper
    {
        public static async Task<ValidationResult> ValidateAllJsonDataAsync()
        {
            var result = new ValidationResult();
            
            // Use the same logic as SeederConfiguration to find the JsonData directory
            var jsonDataPath = FindJsonDataPath();

            try
            {
                // Load all JSON files
                var crossReactivityGroups = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "CrossReactivityGroups.json"));
                var allergens = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "TempAllergens.json"));
                var allergenNames = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "TempAllergenNames.json"));
                var allergenCrossReactivities = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "AllergenCrossReactivities.json"));
                var ingredients = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "Ingredients.json"));
                var ingredientNames = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "IngredientNames.json"));
                var ingredientAllergens = await LoadJsonAsync<List<dynamic>>(Path.Combine(jsonDataPath, "IngredientAllergens.json"));

                // Validate counts
                result.CrossReactivityGroupsCount = crossReactivityGroups?.Count ?? 0;
                result.AllergensCount = allergens?.Count ?? 0;
                result.AllergenNamesCount = allergenNames?.Count ?? 0;
                result.AllergenCrossReactivitiesCount = allergenCrossReactivities?.Count ?? 0;
                result.IngredientsCount = ingredients?.Count ?? 0;
                result.IngredientNamesCount = ingredientNames?.Count ?? 0;
                result.IngredientAllergensCount = ingredientAllergens?.Count ?? 0;

                // Validate relationships
                await ValidateRelationships(result, allergens, allergenNames, allergenCrossReactivities, 
                    crossReactivityGroups, ingredients, ingredientNames, ingredientAllergens);

                result.IsValid = result.ValidationErrors.Count == 0;
            }
            catch (Exception ex)
            {
                result.ValidationErrors.Add($"Error during validation: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private static string FindJsonDataPath()
        {
            // Try to find the JsonData directory relative to the current assembly location
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            
            // Navigate up to find the solution root, then to the JsonData directory
            var currentDir = assemblyDirectory;
            while (currentDir != null && !Directory.Exists(Path.Combine(currentDir, "DrHan.Infrastructure")))
            {
                currentDir = Directory.GetParent(currentDir)?.FullName;
            }
            
            if (currentDir != null)
            {
                var jsonDataPath = Path.Combine(currentDir, "DrHan.Infrastructure", "Seeders", "JsonData");
                if (Directory.Exists(jsonDataPath))
                {
                    return jsonDataPath;
                }
            }
            
            // Fallback to the relative path approach
            return Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "DrHan.Infrastructure", "Seeders", "JsonData");
        }

        private static async Task<T?> LoadJsonAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"JSON file not found: {filePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<T>(jsonContent);
        }

        private static async Task ValidateRelationships(ValidationResult result, 
            List<dynamic>? allergens, List<dynamic>? allergenNames, List<dynamic>? allergenCrossReactivities,
            List<dynamic>? crossReactivityGroups, List<dynamic>? ingredients, List<dynamic>? ingredientNames,
            List<dynamic>? ingredientAllergens)
        {
            await Task.Run(() =>
            {
                // Add validation logic here if needed
                // For now, just basic null checks
                if (allergens == null) result.ValidationErrors.Add("Allergens data is null");
                if (allergenNames == null) result.ValidationErrors.Add("AllergenNames data is null");
                if (allergenCrossReactivities == null) result.ValidationErrors.Add("AllergenCrossReactivities data is null");
                if (crossReactivityGroups == null) result.ValidationErrors.Add("CrossReactivityGroups data is null");
                if (ingredients == null) result.ValidationErrors.Add("Ingredients data is null");
                if (ingredientNames == null) result.ValidationErrors.Add("IngredientNames data is null");
                if (ingredientAllergens == null) result.ValidationErrors.Add("IngredientAllergens data is null");
            });
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> ValidationErrors { get; set; } = new();
            public int CrossReactivityGroupsCount { get; set; }
            public int AllergensCount { get; set; }
            public int AllergenNamesCount { get; set; }
            public int AllergenCrossReactivitiesCount { get; set; }
            public int IngredientsCount { get; set; }
            public int IngredientNamesCount { get; set; }
            public int IngredientAllergensCount { get; set; }

            public override string ToString()
            {
                var summary = $@"
=== Data Validation Summary ===
Valid: {IsValid}
CrossReactivityGroups: {CrossReactivityGroupsCount}
Allergens: {AllergensCount}
AllergenNames: {AllergenNamesCount}
AllergenCrossReactivities: {AllergenCrossReactivitiesCount}
Ingredients: {IngredientsCount}
IngredientNames: {IngredientNamesCount}
IngredientAllergens: {IngredientAllergensCount}

Errors: {ValidationErrors.Count}";

                if (ValidationErrors.Any())
                {
                    summary += "\n" + string.Join("\n", ValidationErrors.Select(e => $"- {e}"));
                }

                return summary;
            }
        }
    }
} 