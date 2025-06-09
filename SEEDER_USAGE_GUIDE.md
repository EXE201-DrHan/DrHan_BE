# DrHan Data Seeder & Cleaner Usage Guide

This guide explains how to use the comprehensive data seeding and cleaning system built for the DrHan project.

## 🌟 Features

- **Generic Seeding System**: Seeds allergen and ingredient data from JSON files
- **Data Cleaning**: Safely removes seeded data respecting foreign key constraints
- **Data Management Service**: High-level API for all data operations
- **Statistics & Health Checks**: Monitor your data status
- **API Endpoints**: RESTful endpoints for data management
- **Automatic Seeding**: Runs on application startup

---

## 📁 Architecture Overview

```
DrHan.Infrastructure/Seeders/
├── GenericJsonSeeder.cs        # Core generic seeder implementation
├── SeederConfiguration.cs     # Centralized file paths and parsers
├── SeederExtensions.cs        # Clean extension methods
├── MasterSeeder.cs            # Orchestrates all seeding operations
├── DataCleaner.cs             # Handles data cleaning/removal
├── DataManagementService.cs   # High-level service for all operations
└── DataValidationHelper.cs    # JSON validation utilities
```

---

## 💾 Data Files

All JSON data files are located in `DrHan.Infrastructure/Seeders/JsonData/`:

- `CrossReactivityGroups.json` - Allergen cross-reactivity groups (6 groups)
- `TempAllergens.json` - Main allergen data (15 allergens)
- `TempAllergenNames.json` - Multilingual allergen names (30 entries)
- `AllergenCrossReactivities.json` - Allergen relationships (15 mappings)
- `Ingredients.json` - Vietnamese ingredients (18 items)
- `IngredientNames.json` - Multilingual ingredient names (36 entries)
- `IngredientAllergens.json` - Ingredient-allergen relationships (19 links)

---

## 🚀 How to Use

### 1. **Automatic Seeding (Recommended)**

The seeder runs automatically when your application starts. It's configured in `Program.cs`:

```csharp
// Automatic seeding after database migration
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    await context.Database.MigrateAsync();
    await MasterSeeder.SeedAllAsync(context, logger);
}
```

### 2. **Using DataManagementService (Dependency Injection)**

Inject the service into your controllers/services:

```csharp
public class YourController : ControllerBase
{
    private readonly DataManagementService _dataService;
    
    public YourController(DataManagementService dataService)
    {
        _dataService = dataService;
    }
    
    public async Task<IActionResult> SomeAction()
    {
        // Seed all data
        await _dataService.SeedAllDataAsync();
        
        // Clean all data
        await _dataService.CleanAllDataAsync();
        
        // Reset (clean + reseed)
        await _dataService.ResetAllDataAsync();
        
        // Get statistics
        var stats = await _dataService.GetDataStatisticsAsync();
        
        return Ok(stats);
    }
}
```

### 3. **Using Extension Methods**

Direct usage with DbContext:

```csharp
using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

// Seed specific entity types
await context.SeedFromJsonAsync<Allergen>(
    SeederConfiguration.TempAllergensPath, 
    SeederConfiguration.ParseAllergens, 
    logger
);

// Or use MasterSeeder
await MasterSeeder.SeedAllAsync(context, logger);
```

### 4. **Using API Endpoints**

The `DataManagementController` provides RESTful endpoints:

#### Seeding Operations
```bash
# Seed all data
POST /api/datamanagement/seed/all

# Seed only allergens
POST /api/datamanagement/seed/allergens

# Seed only ingredients  
POST /api/datamanagement/seed/ingredients
```

#### Cleaning Operations
```bash
# Clean all data
DELETE /api/datamanagement/clean/all

# Reset all data (clean + reseed)
POST /api/datamanagement/reset/all
```

#### Information & Health
```bash
# Get database statistics
GET /api/datamanagement/statistics

# Perform health check
GET /api/datamanagement/health

# Validate JSON files
GET /api/datamanagement/validate

# Ensure data exists (seed if empty)
POST /api/datamanagement/ensure
```

---

## 📊 Available Operations

### **Seeding Operations**
- `SeedAllDataAsync()` - Seeds all 7 entity types
- `SeedAllergenDataAsync()` - Seeds allergen-related data only
- `SeedIngredientDataAsync()` - Seeds ingredient-related data only

### **Cleaning Operations**
- `CleanAllAsync()` - Removes all seeded data (respects FK constraints)
- `CleanAllergenDataAsync()` - Removes allergen-related data only
- `CleanIngredientDataAsync()` - Removes ingredient-related data only
- `CleanEntityAsync<T>()` - Removes specific entity type

### **Combined Operations**
- `ResetAllDataAsync()` - Clean + Reseed all data
- `ResetAllergenDataAsync()` - Clean + Reseed allergen data
- `ResetIngredientDataAsync()` - Clean + Reseed ingredient data

### **Information Operations**
- `GetDataStatisticsAsync()` - Returns count of all entity types
- `ValidateJsonDataAsync()` - Validates all JSON files
- `PerformHealthCheckAsync()` - Complete system health check
- `HasAnyDataAsync()` - Check if database has data
- `HasCompleteDataAsync()` - Check if all required data exists

---

## 🛡️ Safety Features

### **Foreign Key Handling**
The cleaner removes data in reverse dependency order:
1. IngredientAllergens (depends on Ingredients & Allergens)
2. IngredientNames (depends on Ingredients)
3. Ingredients
4. AllergenCrossReactivities (depends on Allergens & CrossReactivityGroups)
5. AllergenNames (depends on Allergens)
6. Allergens
7. CrossReactivityGroups

### **Error Handling**
- All operations include comprehensive try-catch blocks
- Detailed logging for troubleshooting
- Graceful failure with meaningful error messages

### **Validation**
- JSON file existence checking
- JSON syntax validation
- Database connectivity verification
- Entity count validation

---

## 📈 Example Responses

### Statistics Response
```json
{
  "crossReactivityGroupsCount": 6,
  "allergensCount": 15,
  "allergenNamesCount": 30,
  "allergenCrossReactivitiesCount": 15,
  "ingredientsCount": 18,
  "ingredientNamesCount": 36,
  "ingredientAllergensCount": 19,
  "totalRecords": 139
}
```

### Health Check Response
```json
{
  "isHealthy": true,
  "canConnectToDatabase": true,
  "hasData": true,
  "hasCompleteData": true,
  "statistics": { /* ... */ },
  "jsonValidation": {
    "isValid": true,
    "errors": []
  },
  "checkedAt": "2024-01-15T10:30:00Z"
}
```

---

## ⚙️ Configuration

### **File Paths**
All file paths are centralized in `SeederConfiguration.cs`. To add new JSON files:

```csharp
public static class SeederConfiguration
{
    public static string YourNewEntityPath => Path.Combine(JsonDataDirectory, "YourEntity.json");
    
    public static List<YourEntity> ParseYourEntity(string json)
    {
        // Your parsing logic
        return JsonConvert.DeserializeObject<List<YourEntity>>(json) ?? new List<YourEntity>();
    }
}
```

### **Adding New Entity Types**
1. Add JSON file to `JsonData/` directory
2. Add path and parser to `SeederConfiguration.cs`
3. Add seeding call to `MasterSeeder.cs`
4. Add cleaning call to `DataCleaner.cs` (in reverse order)

---

## 🚨 Common Issues & Solutions

### **Issue: "File not found" errors**
- Ensure JSON files exist in `DrHan.Infrastructure/Seeders/JsonData/`
- Check file names match exactly (case-sensitive)

### **Issue: Foreign key constraint errors during cleaning**
- The cleaner handles this automatically by removing in reverse dependency order
- If you see these errors, ensure you're using `DataCleaner.CleanAllAsync()`

### **Issue: Data not seeding**
- Check database connectivity
- Verify JSON file syntax with validation endpoint
- Check logs for detailed error messages

### **Issue: Duplicate data**
- The seeder checks for existing data before seeding
- Use `ResetAllDataAsync()` to clean and reseed

---

## 🎯 Best Practices

1. **Use DataManagementService** for high-level operations
2. **Check health status** before performing operations
3. **Use automatic seeding** in development environments
4. **Validate JSON files** before deploying changes
5. **Monitor logs** for seeding/cleaning operations
6. **Use API endpoints** for manual data management
7. **Test with reset operations** to ensure data integrity

---

## 💡 Tips

- Use `/api/datamanagement/ensure` to safely initialize data
- Check `/api/datamanagement/health` to verify system status
- Use `/api/datamanagement/statistics` to monitor data counts
- The system automatically skips seeding if data already exists
- All operations are logged for debugging purposes

---

## 🔧 Development Workflow

1. **Start Development**: Data seeds automatically
2. **Add New Data**: Update JSON files and restart
3. **Clean Database**: Use clean endpoints or service methods
4. **Reset Data**: Use reset endpoints for fresh start
5. **Validate Changes**: Use health check and statistics endpoints

This system provides a robust, maintainable way to manage your Vietnamese food allergen and ingredient data! 