using DrHan.Application.DTOs.Allergens;
using DrHan.Application.DTOs.Ingredients;
using DrHan.Application.DTOs.Users;
using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Services
{
    public class DetectedFoodDto1
    {
        public string Name { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> PotentialAllergens { get; set; } = new();
        public int? MatchedIngredientId { get; set; }
        public IngredientDto1 MatchedIngredient { get; set; }
    }

    public class IngredientDto1
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<AllergenDto1> Allergens { get; set; } = new();
        public List<string> AlternativeNames { get; set; } = new();
    }

    public class AllergenDto1
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool? IsFdaMajor { get; set; }
        public bool? IsEuMajor { get; set; }
        public List<string> AlternativeNames { get; set; } = new();
    }

    public class AllergenWarningDto1
    {
        public string Allergen { get; set; } = string.Empty;
        public string AllergenDisplayName { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> FoundInFoods { get; set; } = new();
        public int? AllergenId { get; set; }
        public AllergenDto1 AllergenInfo { get; set; }
        public UserAllergyDto1 UserAllergyInfo { get; set; }
        public List<string> EmergencyMedications { get; set; } = new();
        public bool RequiresImmediateAttention { get; set; }
    }

    public class UserAllergyDto1
    {
        public int Id { get; set; }
        public string Severity { get; set; } = string.Empty;
        public DateOnly? DiagnosisDate { get; set; }
        public string DiagnosedBy { get; set; } = string.Empty;
        public DateOnly? LastReactionDate { get; set; }
        public string AvoidanceNotes { get; set; } = string.Empty;
        public bool? Outgrown { get; set; }
        public DateOnly? OutgrownDate { get; set; }
        public List<EmergencyMedicationDto1> EmergencyMedications { get; set; } = new();
    }

    public class EmergencyMedicationDto1
    {
        public int Id { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }

    public class UserAllergyContextDto
    {
        public int? UserId { get; set; }
        public List<UserAllergyDto1> UserAllergies { get; set; } = new();
        public bool HasCriticalAllergies { get; set; }
        public List<string> EmergencyContacts { get; set; } = new();
        public List<EmergencyMedicationDto1> AvailableMedications { get; set; } = new();
    }

    public class FoodAnalysisResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DetectedFoodDto1> DetectedFoods { get; set; } = new();
        public List<AllergenWarningDto1> AllergenWarnings { get; set; } = new();
        public double OverallRiskScore { get; set; }
        public UserAllergyContextDto UserAllergyContext { get; set; }
    }
    public interface IMappingService
    {
        DetectedFoodDto1 MapToDto(DetectedFood detectedFood);
        AllergenWarningDto1 MapToDto(AllergenWarning warning);
        UserAllergyContextDto MapToDto(UserAllergyContext context);
        FoodAnalysisResponseDto MapToDto(FoodAnalysisResponse response);
        List<DetectedFoodDto1> MapToDto(List<DetectedFood> detectedFoods);
        List<AllergenWarningDto1> MapToDto(List<AllergenWarning> warnings);
    }
    public class MappingService : IMappingService
    {
        public DetectedFoodDto1 MapToDto(DetectedFood detectedFood)
        {
            return new DetectedFoodDto1
            {
                Name = detectedFood.Name,
                Confidence = detectedFood.Confidence,
                PotentialAllergens = detectedFood.PotentialAllergens?.ToList() ?? new List<string>(),
                MatchedIngredientId = detectedFood.MatchedIngredientId,
                MatchedIngredient = detectedFood.MatchedIngredient != null ? MapToDto(detectedFood.MatchedIngredient) : null
            };
        }

        public List<DetectedFoodDto1> MapToDto(List<DetectedFood> detectedFoods)
        {
            return detectedFoods?.Select(MapToDto).ToList() ?? new List<DetectedFoodDto1>();
        }

        public AllergenWarningDto1 MapToDto(AllergenWarning warning)
        {
            return new AllergenWarningDto1
            {
                Allergen = warning.Allergen,
                AllergenDisplayName = warning.AllergenDisplayName,
                RiskLevel = warning.RiskLevel,
                Description = warning.Description,
                FoundInFoods = warning.FoundInFoods?.ToList() ?? new List<string>(),
                AllergenId = warning.AllergenId,
                AllergenInfo = warning.AllergenEntity != null ? MapToDto(warning.AllergenEntity) : null,
                UserAllergyInfo = warning.UserAllergyInfo != null ? MapToDto(warning.UserAllergyInfo) : null,
                EmergencyMedications = warning.EmergencyMedications?.ToList() ?? new List<string>(),
                RequiresImmediateAttention = warning.RequiresImmediateAttention
            };
        }

        public List<AllergenWarningDto1> MapToDto(List<AllergenWarning> warnings)
        {
            return warnings?.Select(MapToDto).ToList() ?? new List<AllergenWarningDto1>();
        }

        public UserAllergyContextDto MapToDto(UserAllergyContext context)
        {
            if (context == null) return null;

            return new UserAllergyContextDto
            {
                UserId = context.UserId,
                UserAllergies = context.UserAllergies?.Select(MapToDto).ToList() ?? new List<UserAllergyDto1>(),
                HasCriticalAllergies = context.HasCriticalAllergies,
                EmergencyContacts = context.EmergencyContacts?.ToList() ?? new List<string>(),
                AvailableMedications = context.AvailableMedications?.Select(MapToDto).ToList() ?? new List<EmergencyMedicationDto1>()
            };
        }

        public FoodAnalysisResponseDto MapToDto(FoodAnalysisResponse response)
        {
            return new FoodAnalysisResponseDto
            {
                Success = response.Success,
                Message = response.Message,
                DetectedFoods = MapToDto(response.DetectedFoods),
                AllergenWarnings = MapToDto(response.AllergenWarnings),
                OverallRiskScore = response.OverallRiskScore,
                UserAllergyContext = MapToDto(response.UserAllergyContext)
            };
        }

        private IngredientDto1 MapToDto(Ingredient ingredient)
        {
            return new IngredientDto1
            {
                Id = ingredient.Id,
                Name = ingredient.Name,
                Category = ingredient.Category ?? string.Empty,
                Description = ingredient.Description ?? string.Empty,
                Allergens = ingredient.IngredientAllergens?.Select(ia => MapToDto(ia.Allergen)).ToList() ?? new List<AllergenDto1>(),
                AlternativeNames = ingredient.IngredientNames?.Select(n => n.Name).ToList() ?? new List<string>()
            };
        }

        private AllergenDto1 MapToDto(Allergen allergen)
        {
            return new AllergenDto1
            {
                Id = allergen.Id,
                Name = allergen.Name,
                Category = allergen.Category ?? string.Empty,
                Description = allergen.Description ?? string.Empty,
                IsFdaMajor = allergen.IsFdaMajor,
                IsEuMajor = allergen.IsEuMajor,
                AlternativeNames = allergen.AllergenNames?.Select(n => n.Name).ToList() ?? new List<string>()
            };
        }

        private UserAllergyDto1 MapToDto(UserAllergy userAllergy)
        {
            return new UserAllergyDto1
            {
                Id = userAllergy.Id,
                Severity = userAllergy.Severity ?? string.Empty,
                DiagnosisDate = userAllergy.DiagnosisDate,
                DiagnosedBy = userAllergy.DiagnosedBy ?? string.Empty,
                LastReactionDate = userAllergy.LastReactionDate,
                AvoidanceNotes = userAllergy.AvoidanceNotes ?? string.Empty,
                Outgrown = userAllergy.Outgrown,
                OutgrownDate = userAllergy.OutgrownDate,
                EmergencyMedications = userAllergy.EmergencyMedications?.Select(MapToDto).ToList() ?? new List<EmergencyMedicationDto1>()
            };
        }

        private EmergencyMedicationDto1 MapToDto(EmergencyMedication medication)
        {
            return new EmergencyMedicationDto1
            {
                Id = medication.Id,
                MedicationName = medication.MedicationName ?? string.Empty,
                Dosage = medication.Dosage ?? string.Empty,
                Instructions = medication.Instructions ?? string.Empty
            };
        }
    }
}
