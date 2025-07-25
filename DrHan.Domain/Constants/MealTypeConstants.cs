namespace DrHan.Domain.Constants;

public static class MealTypeConstants
{
    // Normalized meal type values (what gets stored in database)
    public const string BREAKFAST = "Breakfast";
    public const string LUNCH = "Lunch"; 
    public const string DINNER = "Dinner";
    public const string SNACK = "Snack";

    // Mapping for frontend numeric values (1,2,3,4)
    public static readonly Dictionary<string, string> NumericMapping = new()
    {
        { "1", BREAKFAST },
        { "2", LUNCH },
        { "3", DINNER },
        { "4", SNACK }
    };

    // All valid input variations (case-insensitive)
    public static readonly Dictionary<string, string> InputMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // English variations
        { "breakfast", BREAKFAST },
        { "lunch", LUNCH },
        { "dinner", DINNER },
        { "snack", SNACK },
        
        // Vietnamese variations
        { "bữa sáng", BREAKFAST },
        { "sáng", BREAKFAST },
        { "bữa trưa", LUNCH },
        { "trưa", LUNCH },
        { "bữa chiều", DINNER },
        { "chiều", DINNER },
        { "bữa tối", DINNER },
        { "tối", DINNER },
        { "ăn vặt", SNACK },
        { "vặt", SNACK },
        
        // Numeric string mapping
        { "1", BREAKFAST },
        { "2", LUNCH },
        { "3", DINNER },
        { "4", SNACK }
    };

    // All valid normalized meal types
    public static readonly HashSet<string> ValidMealTypes = new()
    {
        BREAKFAST, LUNCH, DINNER, SNACK
    };

    /// <summary>
    /// Normalizes meal type input to standard format
    /// </summary>
    /// <param name="input">Raw meal type input from frontend</param>
    /// <returns>Normalized meal type or null if invalid</returns>
    public static string? NormalizeMealType(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var trimmedInput = input.Trim();
        
        // Try direct mapping first
        if (InputMappings.TryGetValue(trimmedInput, out var normalizedType))
        {
            return normalizedType;
        }

        // If already normalized, return as-is
        if (ValidMealTypes.Contains(trimmedInput))
        {
            return trimmedInput;
        }

        return null;
    }

    /// <summary>
    /// Validates if meal type input is valid
    /// </summary>
    /// <param name="input">Raw meal type input</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidMealType(string? input)
    {
        return NormalizeMealType(input) != null;
    }

    /// <summary>
    /// Gets all supported input formats for documentation
    /// </summary>
    /// <returns>List of all supported meal type inputs</returns>
    public static List<string> GetSupportedInputs()
    {
        return InputMappings.Keys.ToList();
    }

    /// <summary>
    /// Gets mapping for frontend numeric values
    /// </summary>
    /// <returns>Dictionary mapping numbers to meal types</returns>
    public static Dictionary<string, string> GetNumericMapping()
    {
        return new Dictionary<string, string>(NumericMapping);
    }
} 