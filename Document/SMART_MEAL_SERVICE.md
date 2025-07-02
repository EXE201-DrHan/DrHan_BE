# Smart Meal Suggestion Service - How It Works

## Overview

The Smart Meal Suggestion Service is an intelligent meal planning system that automatically recommends and generates personalized meal plans based on user preferences, dietary restrictions, and allergies.

## Core Features

✅ **AI-Powered Recipe Recommendations** - Smart recipe filtering  
✅ **Allergy-Safe Meal Planning** - Automatic allergen exclusion  
✅ **Preference-Based Filtering** - Cuisine, time, budget constraints  
✅ **Meal Type Optimization** - Different algorithms per meal type  
✅ **Bulk Operations** - Generate entire meal plans  
✅ **Intelligent Caching** - High-performance recommendations  

## How It Works

### 1. Smart Generation Process

```
User Request → Validate Input → Load User Allergies → Filter Recipes → Generate Meals → Save Plan
```

**Step 1: Input Validation**
- Validates date ranges and plan types
- Checks family associations for family plans
- Ensures required preferences are provided

**Step 2: User Allergy Loading**
- Queries user's allergen profile from database
- Cache allergies for 2 hours for performance
- Returns list of allergen IDs to exclude

**Step 3: Recipe Filtering**
- Filters recipes by cuisine type preferences
- Excludes recipes with user allergens
- Applies cooking time constraints
- Filters by budget range if specified

**Step 4: Meal Generation**
- Loops through each date in range
- For each meal type (breakfast, lunch, dinner)
- Randomly selects from filtered recipes
- Calculates appropriate serving sizes

**Step 5: Plan Persistence**
- Saves meal plan and all meal entries
- Clears related cache entries
- Returns complete meal plan DTO

### 2. Recipe Filtering Logic

#### Allergy Safety Filter
```csharp
!recipe.RecipeAllergens.Any(ra => userAllergies.Contains(ra.AllergenId ?? 0))
```

#### Cooking Time Filter
```csharp
!preferences.MaxCookingTime.HasValue || 
recipe.CookTimeMinutes <= preferences.MaxCookingTime.Value
```

#### Cuisine Type Filter
```csharp
!preferences.CuisineTypes.Any() || 
preferences.CuisineTypes.Contains(recipe.CuisineType)
```

### 3. Meal Type Optimization

**Breakfast 🌅**
- Priority: Speed (Quick prep time)
- Serving: 1 portion
- Focus: Simple, nutritious starts

**Lunch 🍽️**
- Priority: Portability and moderate prep
- Serving: 1 portion  
- Focus: Balanced, workday-friendly

**Dinner 🌃**
- Priority: Quality and rating
- Serving: 2 portions (with leftovers)
- Focus: Main meal, can be complex

**Snack 🥨**
- Priority: Minimal prep time
- Serving: 1 portion
- Focus: Healthy, quick options

## API Endpoints

### Generate Smart Meal Plan
```http
POST /api/mealplans/generate-smart
```

**Request:**
```json
{
  "name": "Weekly Plan",
  "startDate": "2024-01-15",
  "endDate": "2024-01-21",
  "planType": "Personal",
  "preferences": {
    "cuisineTypes": ["Italian", "Asian"],
    "maxCookingTime": 45,
    "budgetRange": "medium",
    "preferredMealTypes": ["Breakfast", "Lunch", "Dinner"],
    "varietyMode": true
  }
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 123,
    "name": "Weekly Plan",
    "totalMeals": 21,
    "mealEntries": [
      {
        "mealDate": "2024-01-15",
        "mealType": "Breakfast",
        "mealName": "Italian Pancakes",
        "servings": 1,
        "recipeId": 789
      }
    ]
  }
}
```

### Get Recipe Recommendations
```http
POST /api/mealplans/recommendations?mealType=dinner
```

### Generate Smart Meals (for existing plan)
```http
POST /api/mealplans/{mealPlanId}/generate-smart-meals
```

### Bulk Fill Meals
```http
POST /api/mealplans/bulk-fill
```

### Get Generation Options
```http
GET /api/mealplans/smart-generation/options
```

## Smart Algorithms

### Recipe Selection Algorithm
```csharp
private Recipe SelectRandomRecipe(List<Recipe> recipes)
{
    // Random selection from pre-filtered, sorted list
    var random = new Random();
    return recipes[random.Next(recipes.Count)];
}
```

### Pattern-Based Selection
```csharp
private int SelectRecipeByPattern(List<int> recipeIds, string pattern, DateOnly date)
{
    return pattern.ToLower() switch
    {
        "rotate" => recipeIds[date.DayNumber % recipeIds.Count],
        "random" => recipeIds[new Random().Next(recipeIds.Count)],
        "same" => recipeIds.First(),
        _ => recipeIds.First()
    };
}
```

### Serving Calculation
```csharp
private decimal CalculateServings(string mealType)
{
    return mealType.ToLower() switch
    {
        "breakfast" => 1,
        "lunch" => 1,
        "dinner" => 2,    // Plan for leftovers
        "snack" => 1,
        _ => 1
    };
}
```

## Caching Strategy

### Multi-Level Caching System

**User Allergies Cache**
- Duration: 2 hours
- Key: `user_{userId}_allergies`
- Purpose: Avoid repeated allergy queries

**Filtered Recipes Cache**
- Duration: 30 minutes  
- Key: `recipes_filtered_{mealType}_{preferencesHash}`
- Purpose: Cache expensive filtering operations

**Meal Plan Cache**
- Duration: Variable
- Key: `mealplan_{mealPlanId}`
- Purpose: Cache complete meal plan data

## Usage Examples

### Simple Weekly Plan
```javascript
const request = {
  name: "Simple Week",
  startDate: "2024-01-15",
  endDate: "2024-01-21",
  planType: "Personal",
  preferences: {
    maxCookingTime: 30,
    preferredMealTypes: ["Breakfast", "Dinner"],
    varietyMode: true
  }
};
```

### Allergy-Safe Planning
```javascript
// Service automatically excludes allergen-containing recipes
const request = {
  name: "Allergy-Safe Week",
  startDate: "2024-01-15",
  endDate: "2024-01-21",
  preferences: {
    cuisineTypes: ["Italian"],
    maxCookingTime: 45,
    budgetRange: "medium"
  }
};
```

### Family Meal Planning
```javascript
const request = {
  name: "Family Dinners",
  startDate: "2024-01-15",
  endDate: "2024-01-21",
  planType: "Family",
  familyId: 123,
  preferences: {
    preferredMealTypes: ["Dinner"],
    includeLeftovers: true,
    maxCookingTime: 60
  }
};
```

## Performance Features

### Database Optimization
- Selective includes for minimal data transfer
- Complex filters executed at database level
- Indexed columns for fast queries

### Async Operations
- Fire-and-forget cache updates
- Non-blocking background operations
- Graceful error handling

### Fallback Mechanisms
- Simple recipe queries if complex filtering fails
- Default meal selections when preferences too restrictive
- Comprehensive error messages

## Error Handling

### Common Error Scenarios

**No Recipes Found**
```csharp
if (recipesByMealType.Values.All(recipes => !recipes.Any()))
{
    return "No recipes found matching your preferences. Try relaxing constraints.";
}
```

**Authorization Issues**
```csharp
if (mealPlan.UserId != userId)
{
    return "Meal plan not found or access denied";
}
```

**Invalid Configurations**
```csharp
if (planType == "family" && !familyId.HasValue)
{
    return "Family meal plans must be associated with a family";
}
```

## Key Benefits

✅ **Personalized** - Considers individual allergies and preferences  
✅ **Intelligent** - Different algorithms per meal type  
✅ **Safe** - Automatic allergen exclusion  
✅ **Fast** - Multi-level caching and optimization  
✅ **Flexible** - Generate complete plans or fill specific slots  
✅ **User-Friendly** - Comprehensive error messages and fallbacks  

The Smart Meal Suggestion Service balances complexity with performance, providing users with personalized, safe, and delicious meal suggestions while maintaining excellent system performance. 