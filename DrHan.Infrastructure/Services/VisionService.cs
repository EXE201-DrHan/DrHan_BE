using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Users;
using DrHan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Services
{
    public interface IVisionService
    {
        Task<List<AllergenWarning>> AnalyzeAllergensAsync(List<DetectedFood> detectedFoods, List<string> knownAllergies = null, int? userId = null);
        Task<List<Allergen>> GetAllAllergensAsync();
        Task<UserAllergyContext> GetUserAllergyContextAsync(int userId);
        Task<List<Ingredient>> FindMatchingIngredientsAsync(List<string> foodNames);
    }
    public class VisionService : IVisionService
    {
        private readonly ILogger<VisionService> _logger;
        private readonly ApplicationDbContext _context; // Your DbContext
        private readonly Dictionary<string, List<string>> _foodAllergenMap;

        public VisionService(ILogger<VisionService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
            _foodAllergenMap = InitializeFoodAllergenMap();
        }

        public async Task<List<AllergenWarning>> AnalyzeAllergensAsync(List<DetectedFood> detectedFoods, List<string> knownAllergies = null, int? userId = null)
        {
            var warnings = new List<AllergenWarning>();

            // Get user's allergy context if userId is provided
            UserAllergyContext userContext = null;
            if (userId.HasValue)
            {
                userContext = await GetUserAllergyContextAsync(userId.Value);
            }

            // Find matching ingredients in database
            var foodNames = detectedFoods.Select(f => f.Name.ToLower()).ToList();
            var matchingIngredients = await FindMatchingIngredientsAsync(foodNames);

            // Update detected foods with matched ingredients
            foreach (var food in detectedFoods)
            {
                var matchedIngredient = matchingIngredients.FirstOrDefault(i =>
                    i.Name.ToLower().Contains(food.Name.ToLower()) ||
                    i.IngredientNames.Any(n => n.Name.ToLower().Contains(food.Name.ToLower())));

                if (matchedIngredient != null)
                {
                    food.MatchedIngredientId = matchedIngredient.Id;
                    food.MatchedIngredient = matchedIngredient;

                    // Get allergens from database relationships
                    var dbAllergens = matchedIngredient.IngredientAllergens
                        .Select(ia => ia.Allergen.Name.ToLower())
                        .ToList();

                    food.PotentialAllergens.AddRange(dbAllergens);
                }
                else
                {
                    // Fallback to static mapping
                    if (_foodAllergenMap.TryGetValue(food.Name.ToLower(), out var allergens))
                    {
                        food.PotentialAllergens.AddRange(allergens);
                    }
                }
            }

            // Analyze allergens and create warnings
            var allergenCounts = new Dictionary<string, List<string>>();
            var allergenEntities = new Dictionary<string, Allergen>();

            foreach (var food in detectedFoods)
            {
                foreach (var allergen in food.PotentialAllergens)
                {
                    if (!allergenCounts.ContainsKey(allergen))
                        allergenCounts[allergen] = new List<string>();

                    allergenCounts[allergen].Add(food.Name);

                    // Try to get allergen entity from database
                    if (!allergenEntities.ContainsKey(allergen))
                    {
                        var allergenEntity = await _context.Allergens
                            .Include(a => a.AllergenNames)
                            .FirstOrDefaultAsync(a =>
                                a.Name.ToLower() == allergen.ToLower() ||
                                a.AllergenNames.Any(an => an.Name.ToLower() == allergen.ToLower()));

                        if (allergenEntity != null)
                        {
                            allergenEntities[allergen] = allergenEntity;
                        }
                    }
                }
            }

            // Create warnings
            foreach (var allergenGroup in allergenCounts)
            {
                var allergen = allergenGroup.Key;
                var foundInFoods = allergenGroup.Value;
                var allergenEntity = allergenEntities.GetValueOrDefault(allergen);

                UserAllergy userAllergyInfo = null;
                if (userContext != null)
                {
                    userAllergyInfo = userContext.UserAllergies.FirstOrDefault(ua =>
                        ua.Allergen?.Name.ToLower() == allergen.ToLower());
                }

                var riskLevel = DetermineRiskLevel(allergen, foundInFoods, knownAllergies, userAllergyInfo, allergenEntity);

                var warning = new AllergenWarning
                {
                    Allergen = allergen,
                    AllergenDisplayName = allergenEntity?.Name ?? allergen,
                    RiskLevel = riskLevel,
                    Description = allergenEntity?.Description ?? $"Potential {allergen} allergen detected",
                    FoundInFoods = foundInFoods,
                    AllergenId = allergenEntity?.Id,
                    AllergenEntity = allergenEntity,
                    UserAllergyInfo = userAllergyInfo,
                    RequiresImmediateAttention = riskLevel == "Critical"
                };

                // Add emergency medication info if user has this allergy
                if (userAllergyInfo != null)
                {
                    warning.EmergencyMedications = userAllergyInfo.EmergencyMedications
                        .Select(em => em.MedicationName)
                        .ToList();
                }

                warnings.Add(warning);
            }

            return warnings.OrderByDescending(w => GetRiskPriority(w.RiskLevel)).ToList();
        }

        public async Task<List<Allergen>> GetAllAllergensAsync()
        {
            return await _context.Allergens
                .Include(a => a.AllergenNames)
                .Include(a => a.AllergenCrossReactivities)
                .ToListAsync();
        }

        public async Task<UserAllergyContext> GetUserAllergyContextAsync(int userId)
        {
            var userAllergies = await _context.UserAllergies
                .Include(ua => ua.Allergen)
                .ThenInclude(a => a.AllergenNames)
                .Include(ua => ua.EmergencyMedications)
                .Include(ua => ua.UserAllergySymptoms)
                .Where(ua => ua.UserId == userId && ua.Outgrown != true)
                .ToListAsync();

            var hasCriticalAllergies = userAllergies.Any(ua =>
                ua.Severity?.ToLower() == "severe" ||
                ua.Severity?.ToLower() == "critical");

            var allMedications = userAllergies
                .SelectMany(ua => ua.EmergencyMedications)
                .ToList();

            return new UserAllergyContext
            {
                UserId = userId,
                UserAllergies = userAllergies,
                HasCriticalAllergies = hasCriticalAllergies,
                AvailableMedications = allMedications
            };
        }

        public async Task<List<Ingredient>> FindMatchingIngredientsAsync(List<string> foodNames)
        {
            var ingredients = new List<Ingredient>();

            foreach (var foodName in foodNames)
            {
                var matchedIngredients = await _context.Ingredients
                    .Include(i => i.IngredientAllergens)
                    .ThenInclude(ia => ia.Allergen)
                    .Include(i => i.IngredientNames)
                    .Where(i =>
                        i.Name.ToLower().Contains(foodName.ToLower()) ||
                        i.IngredientNames.Any(n => n.Name.ToLower().Contains(foodName.ToLower())))
                    .ToListAsync();

                ingredients.AddRange(matchedIngredients);
            }

            return ingredients.Distinct().ToList();
        }

        private string DetermineRiskLevel(string allergen, List<string> foundInFoods, List<string> knownAllergies,
            UserAllergy userAllergyInfo, Allergen allergenEntity)
        {
            // Critical: User has documented severe allergy
            if (userAllergyInfo != null)
            {
                if (userAllergyInfo.Severity?.ToLower() == "severe" ||
                    userAllergyInfo.Severity?.ToLower() == "critical")
                    return "Critical";

                if (userAllergyInfo.Severity?.ToLower() == "moderate")
                    return "High";

                return "Medium";
            }

            // High: Known allergy from user input
            if (knownAllergies != null && knownAllergies.Contains(allergen, StringComparer.OrdinalIgnoreCase))
                return "High";

            // Check if it's a major allergen (FDA or EU)
            if (allergenEntity != null && (allergenEntity.IsFdaMajor == true || allergenEntity.IsEuMajor == true))
                return "Medium";

            // Common severe allergens (fallback)
            var severeAllergens = new[] { "peanuts", "tree nuts", "shellfish", "fish" };
            if (severeAllergens.Contains(allergen.ToLower()))
                return "Medium";

            return "Low";
        }

        private int GetRiskPriority(string riskLevel)
        {
            return riskLevel switch
            {
                "Critical" => 4,
                "High" => 3,
                "Medium" => 2,
                "Low" => 1,
                _ => 0
            };
        }

        private Dictionary<string, List<string>> InitializeFoodAllergenMap()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Vietnamese ingredients - Dairy products
                { "phô mai", new List<string> { "dairy" } },
                { "sữa", new List<string> { "dairy" } },
                { "bơ", new List<string> { "dairy" } },
                { "kem", new List<string> { "dairy" } },
                { "sữa chua", new List<string> { "dairy" } },
                { "sữa đặc", new List<string> { "dairy" } },
                
                // Gluten sources
                { "bột mì", new List<string> { "gluten" } },
                { "bánh mì", new List<string> { "gluten" } },
                { "mì", new List<string> { "gluten" } },
                { "mì ý", new List<string> { "gluten" } },
                { "bánh quy", new List<string> { "gluten" } },
                { "bánh ngọt", new List<string> { "gluten" } },
                
                // Eggs
                { "trứng gà", new List<string> { "eggs" } },
                { "trứng vịt", new List<string> { "eggs" } },
                { "trứng cút", new List<string> { "eggs" } },
                
                // Peanuts
                { "lạc", new List<string> { "peanuts" } },
                { "đậu phộng", new List<string> { "peanuts" } },
                { "bơ đậu phộng", new List<string> { "peanuts" } },
                
                // Tree nuts
                { "hạt điều", new List<string> { "tree nuts" } },
                { "hạt óc chó", new List<string> { "tree nuts" } },
                { "hạt hạnh nhân", new List<string> { "tree nuts" } },
                { "hạt dẻ", new List<string> { "tree nuts" } },
                
                // Shellfish
                { "tôm", new List<string> { "shellfish" } },
                { "cua", new List<string> { "shellfish" } },
                { "ghẹ", new List<string> { "shellfish" } },
                { "sò", new List<string> { "shellfish" } },
                { "hàu", new List<string> { "shellfish" } },
                { "tôm hùm", new List<string> { "shellfish" } },
                { "mắm tôm", new List<string> { "shellfish" } },
                
                // Fish
                { "cá", new List<string> { "fish" } },
                { "cá hồi", new List<string> { "fish" } },
                { "cá ngừ", new List<string> { "fish" } },
                { "cá rô phi", new List<string> { "fish" } },
                { "mực", new List<string> { "fish" } },
                { "nước mắm", new List<string> { "fish" } },
                
                // Soy products
                { "đậu nành", new List<string> { "soy" } },
                { "đậu phụ", new List<string> { "soy" } },
                { "tương", new List<string> { "soy" } },
                { "nước tương", new List<string> { "soy" } },
                { "giá đỗ", new List<string> { "soy" } },
                { "sữa đậu nành", new List<string> { "soy" } },
                { "tương ớt", new List<string> { "soy" } },
                
                // Sesame
                { "mè", new List<string> { "sesame" } },
                { "vừng", new List<string> { "sesame" } },
                { "dầu mè", new List<string> { "sesame" } },
                
                // English fallbacks
                { "cheese", new List<string> { "dairy" } },
                { "milk", new List<string> { "dairy" } },
                { "wheat flour", new List<string> { "gluten" } },
                { "eggs", new List<string> { "eggs" } },
                { "peanuts", new List<string> { "peanuts" } },
                { "shrimp", new List<string> { "shellfish" } },
                { "fish", new List<string> { "fish" } },
                { "soy", new List<string> { "soy" } },
                { "tofu", new List<string> { "soy" } }
            };
        }
    }
    public class FoodAnalysisRequest
    {
        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> KnownAllergies { get; set; }
        public int? UserId { get; set; } 
    }

    public class FoodAnalysisResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DetectedFood> DetectedFoods { get; set; } = new();
        public List<AllergenWarning> AllergenWarnings { get; set; } = new();
        public double OverallRiskScore { get; set; }
        public UserAllergyContext UserAllergyContext { get; set; } // User-specific context
    }

    public class DetectedFood
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> PotentialAllergens { get; set; } = new();
        public int? MatchedIngredientId { get; set; } // Link to your Ingredient entity
        public Ingredient MatchedIngredient { get; set; }
    }

    public class AllergenWarning
    {
        public string Allergen { get; set; } = string.Empty;
        public string AllergenDisplayName { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High, Critical
        public string Description { get; set; } = string.Empty;
        public List<string> FoundInFoods { get; set; } = new();
        public int? AllergenId { get; set; } // Link to your Allergen entity
        public Allergen AllergenEntity { get; set; }
        public UserAllergy UserAllergyInfo { get; set; } // If user has this allergy
        public List<string> EmergencyMedications { get; set; } = new(); // From user's emergency meds
        public bool RequiresImmediateAttention { get; set; }
    }

    public class UserAllergyContext
    {
        public int? UserId { get; set; }
        public List<UserAllergy> UserAllergies { get; set; } = new();
        public bool HasCriticalAllergies { get; set; }
        public List<string> EmergencyContacts { get; set; } = new();
        public List<EmergencyMedication> AvailableMedications { get; set; } = new();
    }
}
