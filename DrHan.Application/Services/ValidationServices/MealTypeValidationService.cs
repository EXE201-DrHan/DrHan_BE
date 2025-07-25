using DrHan.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace DrHan.Application.Services.ValidationServices;

public interface IMealTypeValidationService
{
    string? NormalizeMealType(string? input);
    bool IsValidMealType(string? input);
    (bool IsValid, string? NormalizedMealType, string? ErrorMessage) ValidateAndNormalize(string? input);
    List<string> GetSupportedInputs();
    Dictionary<string, string> GetNumericMapping();
}

public class MealTypeValidationService : IMealTypeValidationService
{
    private readonly ILogger<MealTypeValidationService> _logger;

    public MealTypeValidationService(ILogger<MealTypeValidationService> logger)
    {
        _logger = logger;
    }

    public string? NormalizeMealType(string? input)
    {
        var normalized = MealTypeConstants.NormalizeMealType(input);
        
        if (normalized == null && !string.IsNullOrWhiteSpace(input))
        {
            _logger.LogWarning("Invalid meal type input: {Input}", input);
        }
        
        return normalized;
    }

    public bool IsValidMealType(string? input)
    {
        return MealTypeConstants.IsValidMealType(input);
    }

    public (bool IsValid, string? NormalizedMealType, string? ErrorMessage) ValidateAndNormalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (false, null, "Meal type is required");
        }

        var normalized = NormalizeMealType(input);
        
        if (normalized == null)
        {
            var supportedInputs = string.Join(", ", GetSupportedInputs().Take(10));
            return (false, null, $"Invalid meal type '{input}'. Supported values include: {supportedInputs}...");
        }

        _logger.LogDebug("Normalized meal type '{Input}' to '{Normalized}'", input, normalized);
        return (true, normalized, null);
    }

    public List<string> GetSupportedInputs()
    {
        return MealTypeConstants.GetSupportedInputs();
    }

    public Dictionary<string, string> GetNumericMapping()
    {
        return MealTypeConstants.GetNumericMapping();
    }
} 