# 🎯 Smart Meal Generation API Documentation

## Overview
The Smart Meal Generation API allows users to automatically generate meals into **existing** meal plans using AI-powered recommendations. This is different from the full meal plan generation which creates a completely new meal plan.

**Base URL**: `/api/mealplans/{mealPlanId}/generate-smart-meals`  
**Authentication**: Bearer Token required  
**Method**: POST

---

## 🚀 Generate Smart Meals into Existing Meal Plan

### Endpoint
**POST** `/api/mealplans/{mealPlanId}/generate-smart-meals`

Generates smart meals into an existing meal plan based on user preferences and dietary restrictions.

### Path Parameters
- `mealPlanId` (integer, required) - ID of the existing meal plan

### Request Body
```json
{
  "preferences": {
    "cuisineTypes": ["Italian", "Asian", "Mediterranean"],
    "maxCookingTime": 45,
    "budgetRange": "medium",
    "dietaryGoals": ["balanced", "protein-rich"],
    "mealComplexity": "moderate",
    "preferredMealTypes": ["breakfast", "lunch", "dinner"],
    "includeLeftovers": true,
    "varietyMode": true
  },
  "targetDates": ["2024-01-01", "2024-01-02", "2024-01-03"],
  "mealTypes": ["Breakfast", "Lunch", "Dinner"],
  "replaceExisting": false,
  "preserveFavorites": true
}
```

### Request Parameters Explained

**preferences** (object) - Smart generation preferences
- `cuisineTypes` (array) - Preferred cuisine types
- `maxCookingTime` (integer) - Maximum cooking time in minutes
- `budgetRange` (string) - Budget preference: "low", "medium", "high"
- `dietaryGoals` (array) - Dietary goals: "balanced", "protein-rich", "low-carb"
- `mealComplexity` (string) - Meal complexity: "simple", "moderate", "complex"
- `preferredMealTypes` (array) - Types of meals to generate
- `includeLeftovers` (boolean) - Whether to include leftover-friendly recipes
- `varietyMode` (boolean) - Ensure meal variety

**targetDates** (array, optional) - Specific dates to generate meals for
- If empty, generates for all dates in the meal plan range
- Format: "YYYY-MM-DD"

**mealTypes** (array, optional) - Specific meal types to generate
- If empty, generates all meal types (breakfast, lunch, dinner)
- Options: "Breakfast", "Lunch", "Dinner", "Snack"

**replaceExisting** (boolean, default: false) - Whether to replace existing meals
- `false` - Only fill empty meal slots
- `true` - Replace existing meals with new smart-generated ones

**preserveFavorites** (boolean, default: true) - Whether to preserve favorite meals
- `true` - Don't replace meals marked as favorites
- `false` - Replace all meals including favorites

### Success Response (200 OK)
```json
{
  "isSucceeded": true,
  "data": {
    "id": 456,
    "name": "My Weekly Meal Plan",
    "startDate": "2024-01-01",
    "endDate": "2024-01-07",
    "planType": "Weekly",
    "notes": "Auto-generated smart meal plan",
    "totalMeals": 21,
    "completedMeals": 0,
    "createdAt": "2024-01-01T10:00:00Z",
    "mealEntries": [
      {
        "id": 789,
        "mealDate": "2024-01-01",
        "mealType": "Breakfast",
        "mealName": "Mediterranean Scrambled Eggs",
        "servings": 1,
        "notes": "Smart-generated",
        "recipeId": 123,
        "recipe": {
          "id": 123,
          "name": "Mediterranean Scrambled Eggs",
          "cookingTime": 15,
          "difficulty": "Easy"
        },
        "isCompleted": false
      }
      // ... more meal entries
    ]
  },
  "messages": {
    "Success": "Successfully generated 12 smart meals"
  }
}
```

### Error Responses

**404 Not Found** - Meal plan not found or access denied
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "NotFound": "Meal plan not found or access denied"
  }
}
```

**400 Bad Request** - Invalid request data
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "Validation": "Invalid meal plan preferences"
  }
}
```

**401 Unauthorized** - Authentication required
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "Authorization": "Authentication required"
  }
}
```

---

## 🎯 Usage Examples

### Example 1: Fill Empty Slots Only
```bash
curl -X POST "https://api.drhan.com/api/mealplans/456/generate-smart-meals" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "preferences": {
      "cuisineTypes": ["Italian", "Mediterranean"],
      "maxCookingTime": 30,
      "mealComplexity": "simple"
    },
    "replaceExisting": false,
    "preserveFavorites": true
  }'
```

### Example 2: Generate Only Breakfast for Specific Dates
```bash
curl -X POST "https://api.drhan.com/api/mealplans/456/generate-smart-meals" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "preferences": {
      "maxCookingTime": 15,
      "mealComplexity": "simple"
    },
    "targetDates": ["2024-01-01", "2024-01-02", "2024-01-03"],
    "mealTypes": ["Breakfast"],
    "replaceExisting": true
  }'
```

### Example 3: Full Week Smart Generation with Dietary Restrictions
```bash
curl -X POST "https://api.drhan.com/api/mealplans/456/generate-smart-meals" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "preferences": {
      "cuisineTypes": ["Asian", "Mediterranean"],
      "maxCookingTime": 45,
      "budgetRange": "medium",
      "dietaryGoals": ["balanced", "low-carb"],
      "mealComplexity": "moderate",
      "varietyMode": true
    },
    "mealTypes": ["Breakfast", "Lunch", "Dinner"],
    "replaceExisting": false,
    "preserveFavorites": true
  }'
```

---

## 🔄 Smart Generation Logic

### How It Works:
1. **Validates** meal plan ownership and existence
2. **Fetches** user allergies and dietary restrictions
3. **Determines** target dates (user-specified or full meal plan range)
4. **Filters** recipes based on preferences and allergies
5. **Checks** existing meals and respects replacement settings
6. **Generates** smart meal selections using AI algorithms
7. **Updates** meal plan with new entries
8. **Returns** updated meal plan with generated meals

### Smart Features:
- **Allergy-Aware**: Automatically excludes recipes with user allergens
- **Preference-Based**: Filters by cuisine, cooking time, complexity
- **Variety Algorithm**: Ensures meal diversity across dates
- **Favorite Protection**: Preserves user-marked favorite meals
- **Flexible Targeting**: Generate for specific dates/meal types only
- **Cache Optimization**: Uses intelligent caching for performance

---

## 🆚 Comparison with Full Meal Plan Generation

| Feature | Smart Meals Generation | Full Meal Plan Generation |
|---------|----------------------|---------------------------|
| **Target** | Existing meal plan | Creates new meal plan |
| **Endpoint** | `POST /{id}/generate-smart-meals` | `POST /generate-smart` |
| **Flexibility** | Partial generation, date-specific | Full plan only |
| **Existing Data** | Preserves/replaces existing meals | Starts fresh |
| **Use Case** | Fill gaps, refresh meals | Complete new plan |

---

## ⚙️ Get Smart Generation Options

### Endpoint
**GET** `/api/mealplans/smart-generation/options`

Get all available options that users can pick when generating smart meals or meal plans. This endpoint helps frontend applications build user interfaces with the correct option values.

### Request
No request body required. This is a simple GET endpoint.

### Response
```json
{
  "isSucceeded": true,
  "data": {
    "availableCuisineTypes": [
      "Italian", "Asian", "Mediterranean", "Mexican", "American",
      "French", "Thai", "Indian", "Chinese", "Japanese",
      "Korean", "Vietnamese", "Greek", "Spanish", "Middle Eastern"
    ],
    "budgetRangeOptions": ["low", "medium", "high"],
    "dietaryGoalOptions": ["balanced", "protein-rich", "low-carb", "high-fiber", "low-sodium"],
    "mealComplexityOptions": ["simple", "moderate", "complex"],
    "mealTypeOptions": ["breakfast", "lunch", "dinner", "snack"],
    "planTypeOptions": ["Personal", "Family", "Weekly", "Monthly"],
    "fillPatternOptions": ["rotate", "random", "same"],
    "cookingTimeRange": {
      "minCookingTime": 5,
      "maxCookingTime": 180,
      "defaultMaxCookingTime": 45,
      "recommendedTimeRanges": [15, 30, 45, 60, 90]
    },
    "optionDescriptions": {
      "budgetRange.low": "Budget-friendly recipes with affordable ingredients",
      "budgetRange.medium": "Balanced cost recipes with quality ingredients",
      "budgetRange.high": "Premium recipes with high-quality or specialty ingredients",
      "mealComplexity.simple": "Easy recipes with minimal prep and cooking steps",
      "mealComplexity.moderate": "Recipes with moderate prep time and cooking techniques",
      "mealComplexity.complex": "Advanced recipes requiring more time and cooking skills",
      "dietaryGoals.balanced": "Well-rounded meals with balanced macronutrients",
      "dietaryGoals.protein-rich": "High-protein meals for muscle building and satiety",
      "dietaryGoals.low-carb": "Low-carbohydrate meals for weight management",
      "dietaryGoals.high-fiber": "High-fiber meals for digestive health",
      "dietaryGoals.low-sodium": "Low-sodium meals for heart health",
      "fillPattern.rotate": "Cycle through recipes in order across dates",
      "fillPattern.random": "Randomly select from available recipes",
      "fillPattern.same": "Use the same recipe for all selected slots",
      "includeLeftovers": "Include recipes that work well as leftovers for meal prep",
      "varietyMode": "Ensure variety by avoiding repetitive meals within the plan",
      "replaceExisting": "Replace existing meals in the plan with new smart-generated ones",
      "preserveFavorites": "Keep meals marked as favorites and don't replace them"
    }
  },
  "messages": {
    "Success": "Smart generation options retrieved successfully"
  }
}
```

### Usage Examples

**Frontend Integration:**
```javascript
// Fetch available options
const response = await fetch('/api/mealplans/smart-generation/options', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const { data: options } = await response.json();

// Build UI dropdowns
const cuisineSelect = options.availableCuisineTypes.map(cuisine => ({
  value: cuisine,
  label: cuisine
}));
```

### Status Codes
- `200 OK` - Options retrieved successfully
- `401 Unauthorized` - Authentication required
- `500 Internal Server Error` - Server error

---

## 🛡️ Security & Permissions

- **Authentication**: Bearer token required
- **Authorization**: User can only access their own meal plans
- **Rate Limiting**: Standard API rate limits apply
- **Data Validation**: All input parameters are validated

---

## 📚 Related Endpoints

- [Create Meal Plan](./MEALPLAN_API.md#create-meal-plan) - Create empty meal plan first
- [Get Meal Plans](./MEALPLAN_API.md#get-meal-plans) - List user's meal plans
- [Recipe Search](./RECIPE_API.md) - Search recipes manually
- [User Allergies](./USER_ALLERGY_API.md) - Manage user allergies

---

## ⚙️ Get Smart Generation Options

### Endpoint
**GET** `/api/mealplans/smart-generation/options`

Get all available options that users can pick when generating smart meals or meal plans. This endpoint helps frontend applications build user interfaces with the correct option values.

### Request
No request body required. This is a simple GET endpoint.

### Response
```json
{
  "isSucceeded": true,
  "data": {
    "availableCuisineTypes": [
      "Italian", "Asian", "Mediterranean", "Mexican", "American",
      "French", "Thai", "Indian", "Chinese", "Japanese",
      "Korean", "Vietnamese", "Greek", "Spanish", "Middle Eastern"
    ],
    "budgetRangeOptions": ["low", "medium", "high"],
    "dietaryGoalOptions": ["balanced", "protein-rich", "low-carb", "high-fiber", "low-sodium"],
    "mealComplexityOptions": ["simple", "moderate", "complex"],
    "mealTypeOptions": ["breakfast", "lunch", "dinner", "snack"],
    "planTypeOptions": ["Personal", "Family", "Weekly", "Monthly"],
    "fillPatternOptions": ["rotate", "random", "same"],
    "cookingTimeRange": {
      "minCookingTime": 5,
      "maxCookingTime": 180,
      "defaultMaxCookingTime": 45,
      "recommendedTimeRanges": [15, 30, 45, 60, 90]
    },
    "optionDescriptions": {
      "budgetRange.low": "Budget-friendly recipes with affordable ingredients",
      "budgetRange.medium": "Balanced cost recipes with quality ingredients",
      "budgetRange.high": "Premium recipes with high-quality or specialty ingredients",
      "mealComplexity.simple": "Easy recipes with minimal prep and cooking steps",
      "mealComplexity.moderate": "Recipes with moderate prep time and cooking techniques",
      "mealComplexity.complex": "Advanced recipes requiring more time and cooking skills",
      "dietaryGoals.balanced": "Well-rounded meals with balanced macronutrients",
      "dietaryGoals.protein-rich": "High-protein meals for muscle building and satiety",
      "dietaryGoals.low-carb": "Low-carbohydrate meals for weight management",
      "dietaryGoals.high-fiber": "High-fiber meals for digestive health",
      "dietaryGoals.low-sodium": "Low-sodium meals for heart health",
      "fillPattern.rotate": "Cycle through recipes in order across dates",
      "fillPattern.random": "Randomly select from available recipes",
      "fillPattern.same": "Use the same recipe for all selected slots",
      "includeLeftovers": "Include recipes that work well as leftovers for meal prep",
      "varietyMode": "Ensure variety by avoiding repetitive meals within the plan",
      "replaceExisting": "Replace existing meals in the plan with new smart-generated ones",
      "preserveFavorites": "Keep meals marked as favorites and don't replace them"
    }
  },
  "messages": {
    "Success": "Smart generation options retrieved successfully"
  }
}
```

### Response Fields Explained

**availableCuisineTypes** (array) - All supported cuisine types
- Use these values in the `cuisineTypes` array when making generation requests

**budgetRangeOptions** (array) - Available budget levels
- `"low"` - Budget-friendly recipes
- `"medium"` - Balanced cost recipes 
- `"high"` - Premium ingredient recipes

**dietaryGoalOptions** (array) - Supported dietary goals
- Can combine multiple goals in generation requests
- Each goal filters recipes to match nutritional objectives

**mealComplexityOptions** (array) - Recipe complexity levels
- `"simple"` - Quick and easy recipes
- `"moderate"` - Standard cooking complexity
- `"complex"` - Advanced cooking techniques

**mealTypeOptions** (array) - Meal categories
- Use in `mealTypes` or `preferredMealTypes` arrays

**planTypeOptions** (array) - Meal plan categories
- Used when creating new meal plans

**fillPatternOptions** (array) - Patterns for bulk operations
- `"rotate"` - Cycle through recipes in order
- `"random"` - Random selection each time
- `"same"` - Use same recipe for all slots

**cookingTimeRange** (object) - Cooking time constraints
- `minCookingTime` - Minimum supported cooking time
- `maxCookingTime` - Maximum supported cooking time  
- `defaultMaxCookingTime` - Recommended default value
- `recommendedTimeRanges` - Common time limit choices

**optionDescriptions** (object) - User-friendly descriptions
- Key format: `"{category}.{value}"` or `"{booleanOption}"`
- Values provide explanatory text for UI tooltips/help

### Usage Examples

**Basic Frontend Integration:**
```javascript
// Fetch available options
const response = await fetch('/api/mealplans/smart-generation/options', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const { data: options } = await response.json();

// Build UI dropdowns
const cuisineSelect = options.availableCuisineTypes.map(cuisine => ({
  value: cuisine,
  label: cuisine
}));

const budgetSelect = options.budgetRangeOptions.map(budget => ({
  value: budget,
  label: options.optionDescriptions[`budgetRange.${budget}`]
}));
```

**Validation Helper:**
```javascript
function validateGenerationRequest(request, options) {
  const errors = [];
  
  if (request.preferences?.budgetRange && 
      !options.budgetRangeOptions.includes(request.preferences.budgetRange)) {
    errors.push('Invalid budget range');
  }
  
  if (request.preferences?.mealComplexity && 
      !options.mealComplexityOptions.includes(request.preferences.mealComplexity)) {
    errors.push('Invalid meal complexity');
  }
  
  return errors;
}
```

### Caching
- Options are cached for 6 hours to improve performance
- Cache automatically invalidates when new options are added
- No authentication required, but standard rate limits apply

### Status Codes
- `200 OK` - Options retrieved successfully
- `401 Unauthorized` - Authentication required (Bearer token)
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

---

This endpoint is essential for building user-friendly interfaces that guide users through the smart meal generation process with valid option values and helpful descriptions.

This new API endpoint provides powerful flexibility for users to enhance their existing meal plans with AI-powered smart meal generation! 