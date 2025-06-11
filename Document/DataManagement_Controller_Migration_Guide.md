# DataManagement Controller Migration Guide

## Overview
This guide shows how to complete the migration of all DataManagement endpoints to use the `AppResponse<T>` wrapper format for consistency with the Authentication API.

## What's Been Done
✅ Updated imports to include `AppResponse` and DTOs  
✅ Migrated these endpoints:
- `POST /seed/all` → `AppResponse<SeedDataResponse>`
- `POST /seed/users` → `AppResponse<SeedUsersResponse>`  
- `GET /statistics` → `AppResponse<StatisticsResponse>`
- `GET /health` → `AppResponse<HealthCheckResponse>`
- `GET /validate` → `AppResponse<ValidationResponse>`

✅ Created helper methods for DTO conversion  
✅ Updated API documentation format

## Remaining Endpoints to Migrate

### 1. Seed Endpoints
```csharp
// Current Pattern:
[HttpPost("seed/allergens")]
public async Task<IActionResult> SeedAllergenData()

// Should become:
[HttpPost("seed/allergens")]
public async Task<ActionResult<AppResponse<SeedDataResponse>>> SeedAllergenData()
{
    try
    {
        await _dataManagementService.SeedAllergenDataAsync();
        var response = new AppResponse<SeedDataResponse>()
            .SetSuccessResponse(new SeedDataResponse
            {
                Message = "Allergen data seeded successfully",
                Operation = "SeedAllergens",
                RecordsAffected = 0, // TODO: Get actual count
                Timestamp = DateTime.UtcNow
            });
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to seed allergen data");
        var errorResponse = new AppResponse<SeedDataResponse>()
            .SetErrorResponse("SeedOperation", "Failed to seed allergen data")
            .SetErrorResponse("Details", ex.Message);
        return StatusCode(500, errorResponse);
    }
}
```

**Apply this pattern to:**
- `POST /seed/ingredients` → `AppResponse<SeedDataResponse>`
- `POST /seed/food` → `AppResponse<SeedDataResponse>`

### 2. Clean Endpoints
```csharp
// Pattern for clean endpoints:
[HttpDelete("clean/all")]
public async Task<ActionResult<AppResponse<CleanDataResponse>>> CleanAllData()
{
    try
    {
        await _dataManagementService.CleanAllDataAsync();
        var response = new AppResponse<CleanDataResponse>()
            .SetSuccessResponse(new CleanDataResponse
            {
                Message = "All data cleaned successfully",
                Operation = "CleanAll",
                RecordsRemoved = 0, // TODO: Get actual count
                Timestamp = DateTime.UtcNow
            });
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to clean all data");
        var errorResponse = new AppResponse<CleanDataResponse>()
            .SetErrorResponse("CleanOperation", "Failed to clean all data")
            .SetErrorResponse("Details", ex.Message);
        return StatusCode(500, errorResponse);
    }
}
```

**Apply this pattern to:**
- `DELETE /clean/all` → `AppResponse<CleanDataResponse>`
- `DELETE /clean/food` → `AppResponse<CleanDataResponse>`  
- `DELETE /clean/users` → `AppResponse<CleanDataResponse>`

### 3. Reset Endpoints
```csharp
// Pattern for reset endpoints:
[HttpPost("reset/all")]
public async Task<ActionResult<AppResponse<ResetDataResponse>>> ResetAllData()
{
    try
    {
        await _dataManagementService.ResetAllDataAsync();
        var response = new AppResponse<ResetDataResponse>()
            .SetSuccessResponse(new ResetDataResponse
            {
                Message = "All data reset successfully",
                Operation = "ResetAll",
                RecordsRemoved = 0, // TODO: Get actual count
                RecordsAdded = 0,   // TODO: Get actual count
                Timestamp = DateTime.UtcNow
            });
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to reset all data");
        var errorResponse = new AppResponse<ResetDataResponse>()
            .SetErrorResponse("ResetOperation", "Failed to reset all data")
            .SetErrorResponse("Details", ex.Message);
        return StatusCode(500, errorResponse);
    }
}
```

**Apply this pattern to:**
- `POST /reset/all` → `AppResponse<ResetDataResponse>`
- `POST /reset/food` → `AppResponse<ResetDataResponse>`
- `POST /reset/users` → `AppResponse<ResetUsersResponse>` (special case with user stats)

### 4. Utility Endpoints
```csharp
// Pattern for utility endpoints:
[HttpGet("statistics/users")]
public async Task<ActionResult<AppResponse<UserStatistics>>> GetUserStatistics()
{
    try
    {
        var userStats = await _dataManagementService.GetUserStatisticsAsync();
        var response = new AppResponse<UserStatistics>()
            .SetSuccessResponse(ConvertToUserStatisticsDto(userStats));
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to get user statistics");
        var errorResponse = new AppResponse<UserStatistics>()
            .SetErrorResponse("Statistics", "Failed to retrieve user statistics")
            .SetErrorResponse("Details", ex.Message);
        return StatusCode(500, errorResponse);
    }
}

[HttpPost("ensure")]
public async Task<ActionResult<AppResponse<EnsureDataResponse>>> EnsureData()
{
    try
    {
        await _dataManagementService.EnsureDataAsync();
        var stats = await _dataManagementService.GetDataStatisticsAsync();
        var response = new AppResponse<EnsureDataResponse>()
            .SetSuccessResponse(new EnsureDataResponse
            {
                Message = "Data ensured successfully",
                Statistics = ConvertToStatisticsDto(stats),
                Action = "data_ensured", // TODO: Get actual action from service
                Timestamp = DateTime.UtcNow
            });
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to ensure data");
        var errorResponse = new AppResponse<EnsureDataResponse>()
            .SetErrorResponse("EnsureOperation", "Failed to ensure data")
            .SetErrorResponse("Details", ex.Message);
        return StatusCode(500, errorResponse);
    }
}
```

**Apply this pattern to:**
- `GET /statistics/users` → `AppResponse<UserStatistics>`
- `POST /ensure` → `AppResponse<EnsureDataResponse>`

## Steps to Complete Migration

### 1. Update All Controller Methods
Replace each method signature and implementation using the patterns above.

### 2. Update API Documentation
For each endpoint, update the response examples in `DATAMANAGEMENT_API.md` to use the new format:

```json
// OLD FORMAT
{
  "message": "Operation successful",
  "timestamp": "2024-01-15T10:30:00Z"
}

// NEW FORMAT  
{
  "isSucceeded": true,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {},
  "data": {
    "message": "Operation successful",
    "operation": "OperationName",
    "recordsAffected": 100,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "pagination": null
}
```

### 3. Enhance Service Layer (Optional)
To get actual record counts, consider enhancing the DataManagementService methods to return operation results:

```csharp
public class OperationResult
{
    public bool Success { get; set; }
    public int RecordsAffected { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }
}

// Example enhanced service method:
public async Task<OperationResult> SeedAllDataAsync()
{
    var result = new OperationResult();
    try
    {
        // Seeding logic...
        result.Success = true;
        result.RecordsAffected = totalRecordsSeeded;
        result.Message = "All data seeded successfully";
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Exception = ex;
        result.Message = "Failed to seed data";
    }
    return result;
}
```

## Benefits of This Migration

1. **Consistency**: All APIs use the same response format
2. **Type Safety**: Strong typing with `AppResponse<T>`
3. **Error Handling**: Standardized error response structure
4. **Client Development**: Easier to consume on frontend
5. **Documentation**: Clear, consistent API documentation
6. **Debugging**: Better error categorization and details

## Validation

After migration, test each endpoint to ensure:
- ✅ Success responses have `isSucceeded: true`
- ✅ Error responses have `isSucceeded: false` with proper error messages
- ✅ All responses include timestamp
- ✅ Data structure matches the DTO definitions
- ✅ HTTP status codes are appropriate (200 for success, 500 for errors) 