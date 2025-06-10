namespace DrHan.Application.DTOs.DataManagement
{
    public class BaseDataManagementResponse
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SeedDataResponse : BaseDataManagementResponse
    {
        public string Operation { get; set; } = string.Empty;
        public int RecordsAffected { get; set; }
    }

    public class SeedUsersResponse : BaseDataManagementResponse
    {
        public UserStatistics? UserStatistics { get; set; }
    }

    public class CleanDataResponse : BaseDataManagementResponse
    {
        public string Operation { get; set; } = string.Empty;
        public int RecordsRemoved { get; set; }
    }

    public class ResetDataResponse : BaseDataManagementResponse
    {
        public string Operation { get; set; } = string.Empty;
        public int RecordsRemoved { get; set; }
        public int RecordsAdded { get; set; }
    }

    public class ResetUsersResponse : BaseDataManagementResponse
    {
        public UserStatistics? UserStatistics { get; set; }
        public int RecordsRemoved { get; set; }
        public int RecordsAdded { get; set; }
    }

    public class StatisticsResponse
    {
        public int CrossReactivityGroupsCount { get; set; }
        public int AllergensCount { get; set; }
        public int AllergenNamesCount { get; set; }
        public int IngredientsCount { get; set; }
        public int IngredientNamesCount { get; set; }
        public int AllergenIngredientRelationsCount { get; set; }
        public int TotalRecords { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public int AdminUsers { get; set; }
        public int StaffUsers { get; set; }
        public int NutritionistUsers { get; set; }
        public int CustomerUsers { get; set; }
        public int TotalRoles { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class HealthCheckResponse
    {
        public bool IsHealthy { get; set; }
        public bool CanConnectToDatabase { get; set; }
        public bool HasData { get; set; }
        public bool HasCompleteData { get; set; }
        public StatisticsResponse? Statistics { get; set; }
        public ValidationResponse? JsonValidation { get; set; }
        public string? Error { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }

    public class ValidationResponse
    {
        public bool IsValid { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public int FilesChecked { get; set; }
        public int ValidFiles { get; set; }
        public int InvalidFiles { get; set; }
        public Dictionary<string, string> Details { get; set; } = new Dictionary<string, string>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class EnsureDataResponse : BaseDataManagementResponse
    {
        public StatisticsResponse? Statistics { get; set; }
        public string Action { get; set; } = string.Empty; // "seeded_all", "seeded_users", "seeded_food", "no_action"
    }
} 