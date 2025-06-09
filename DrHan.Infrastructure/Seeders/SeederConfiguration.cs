using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.Ingredients;
using System.Reflection;
using System.Text.Json;

namespace DrHan.Infrastructure.Seeders
{
    public static class SeederConfiguration
    {
        private static string JsonDataBasePath =>
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DrHan.Infrastructure", "Seeders", "JsonData");

        public static class FilePaths
        {
            public static string CrossReactivityGroups => Path.Combine(JsonDataBasePath, "CrossReactivityGroups.json");
            public static string Allergens => Path.Combine(JsonDataBasePath, "TempAllergens.json");
            public static string AllergenNames => Path.Combine(JsonDataBasePath, "TempAllergenNames.json");
            public static string AllergenCrossReactivities => Path.Combine(JsonDataBasePath, "AllergenCrossReactivities.json");
            public static string Ingredients => Path.Combine(JsonDataBasePath, "Ingredients.json");
            public static string IngredientNames => Path.Combine(JsonDataBasePath, "IngredientNames.json");
            public static string IngredientAllergens => Path.Combine(JsonDataBasePath, "IngredientAllergens.json");
        }

        public static class JsonParsers
        {
            public static async Task<List<CrossReactivityGroup>> ParseCrossReactivityGroups(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<CrossReactivityGroupDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new CrossReactivityGroup
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Description = dto.Description,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<CrossReactivityGroup>());
            }

            public static async Task<List<Allergen>> ParseAllergens(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<AllergenDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new Allergen
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Category = dto.Category,
                    ScientificName = dto.ScientificName,
                    Description = dto.Description,
                    IsFdaMajor = dto.IsFdaMajor,
                    IsEuMajor = dto.IsEuMajor,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<Allergen>());
            }

            public static async Task<List<AllergenName>> ParseAllergenNames(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<AllergenNameDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new AllergenName
                {
                    Id = dto.Id,
                    AllergenId = dto.AllergenId,
                    Name = dto.Name,
                    IsPrimary = dto.IsPrimary,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<AllergenName>());
            }

            public static async Task<List<AllergenCrossReactivity>> ParseAllergenCrossReactivities(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<AllergenCrossReactivityDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new AllergenCrossReactivity
                {
                    Id = dto.Id,
                    AllergenId = dto.AllergenId,
                    GroupId = dto.GroupId,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<AllergenCrossReactivity>());
            }

            public static async Task<List<Ingredient>> ParseIngredients(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<IngredientDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new Ingredient
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Category = dto.Category,
                    Description = dto.Description,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<Ingredient>());
            }

            public static async Task<List<IngredientName>> ParseIngredientNames(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<IngredientNameDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new IngredientName
                {
                    Id = dto.Id,
                    IngredientId = dto.IngredientId,
                    Name = dto.Name,
                    IsPrimary = dto.IsPrimary,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<IngredientName>());
            }

            public static async Task<List<IngredientAllergen>> ParseIngredientAllergens(string jsonContent)
            {
                var dtos = JsonSerializer.Deserialize<List<IngredientAllergenDto>>(jsonContent);
                return await Task.FromResult(dtos?.Select(dto => new IngredientAllergen
                {
                    Id = dto.Id,
                    IngredientId = dto.IngredientId,
                    AllergenId = dto.AllergenId,
                    AllergenType = dto.AllergenType,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                }).ToList() ?? new List<IngredientAllergen>());
            }
        }

        // DTOs for JSON deserialization
        private class CrossReactivityGroupDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private class AllergenDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string? ScientificName { get; set; }
            public string Description { get; set; } = string.Empty;
            public bool IsFdaMajor { get; set; }
            public bool IsEuMajor { get; set; }
        }

        private class AllergenNameDto
        {
            public int Id { get; set; }
            public int AllergenId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsPrimary { get; set; }
        }

        private class AllergenCrossReactivityDto
        {
            public int Id { get; set; }
            public int AllergenId { get; set; }
            public int GroupId { get; set; }
        }

        private class IngredientDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private class IngredientNameDto
        {
            public int Id { get; set; }
            public int IngredientId { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsPrimary { get; set; }
        }

        private class IngredientAllergenDto
        {
            public int Id { get; set; }
            public int IngredientId { get; set; }
            public int AllergenId { get; set; }
            public string AllergenType { get; set; } = string.Empty;
        }
    }
} 