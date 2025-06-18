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

## 🛡️ Security & Permissions

- **Authentication**: Bearer token required
- **Authorization**: User must own the meal plan
- **Validation**: All input parameters validated
- **Rate Limiting**: Smart generation calls are rate-limited
- **Error Handling**: Comprehensive error responses

---

## 🚀 Status Codes

- `200 OK` - Meals generated successfully
- `400 Bad Request` - Invalid request parameters
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions  
- `404 Not Found` - Meal plan not found
- `422 Unprocessable Entity` - Invalid meal plan state
- `500 Internal Server Error` - Server error

This new API endpoint provides powerful flexibility for users to enhance their existing meal plans with AI-powered smart meal generation! 