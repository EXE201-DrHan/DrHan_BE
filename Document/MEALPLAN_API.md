# 🍽️ Meal Plan API Documentation

## Overview
The Meal Plan API provides comprehensive functionality for creating, managing, and automatically generating meal plans. It supports both manual meal planning and AI-powered smart meal plan generation with personalized recommendations.

**Base URL**: `/api/mealplans`  
**Authentication**: Bearer Token required for all endpoints

---

## 📋 Table of Contents
1. [Smart Generation Endpoints](#smart-generation-endpoints)
2. [Manual Meal Plan Management](#manual-meal-plan-management)
3. [Query Endpoints](#query-endpoints)
4. [Update & Delete Operations](#update--delete-operations)
5. [Data Models](#data-models)
6. [Error Handling](#error-handling)
7. [Performance & Caching](#performance--caching)

---

## 🎯 Smart Generation Endpoints

### Generate Smart Meal Plan
**POST** `/api/mealplans/generate-smart`

Automatically generates a complete meal plan based on user preferences, dietary restrictions, and AI recommendations.

**Request Body:**
```json
{
  "name": "My Weekly Meal Plan",
  "startDate": "2024-01-01",
  "endDate": "2024-01-07",
  "planType": "Weekly",
  "familyId": 123,
  "preferences": {
    "cuisineTypes": ["Italian", "Asian", "Mediterranean"],
    "maxCookingTime": 45,
    "budgetRange": "medium",
    "dietaryGoals": ["balanced", "protein-rich"],
    "mealComplexity": "moderate",
    "preferredMealTypes": ["breakfast", "lunch", "dinner"],
    "includeLeftovers": true,
    "varietyMode": true
  }
}
```

**Response:**
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
        "recipeId": 123,
        "recipe": {
          "id": 123,
          "name": "Mediterranean Scrambled Eggs",
          "cookingTime": 15,
          "difficulty": "Easy"
        },
        "isCompleted": false
      }
    ]
  },
  "messages": {
    "Success": "Smart meal plan generated successfully with 21 meals"
  }
}
```

**Status Codes:**
- `201 Created` - Meal plan generated successfully
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `500 Internal Server Error` - Server error

---

### Get Recipe Recommendations
**POST** `/api/mealplans/recommendations`

Get personalized recipe recommendations based on user preferences and dietary restrictions.

**Query Parameters:**
- `mealType` (optional) - Type of meal (breakfast, lunch, dinner, snack). Default: "dinner"

**Request Body:**
```json
{
  "cuisineTypes": ["Italian", "Asian"],
  "maxCookingTime": 30,
  "budgetRange": "medium",
  "dietaryGoals": ["balanced"],
  "mealComplexity": "simple",
  "preferredMealTypes": ["dinner"],
  "includeLeftovers": true,
  "varietyMode": true
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [123, 456, 789, 101],
  "messages": {
    "Success": "Found 4 recommended recipes"
  }
}
```

---

### Get Available Templates
**GET** `/api/mealplans/templates`

Retrieve predefined meal plan templates for quick meal plan creation.

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Busy Professional Week",
      "description": "Quick 15-minute meals for busy weekdays",
      "category": "Quick & Easy",
      "duration": 7,
      "mealStructure": {
        "breakfast": [101, 102, 103],
        "lunch": [201, 202, 203],
        "dinner": [301, 302, 303]
      }
    }
  ],
  "messages": {
    "Success": "Templates retrieved successfully"
  }
}
```

---

### Bulk Fill Meals
**POST** `/api/mealplans/bulk-fill`

Efficiently fill multiple meal slots with selected recipes using various patterns.

**Request Body:**
```json
{
  "mealPlanId": 456,
  "mealType": "dinner",
  "fillPattern": "rotate",
  "recipeIds": [123, 456, 789],
  "targetDates": ["2024-01-01", "2024-01-02", "2024-01-03"]
}
```

**Fill Patterns:**
- `rotate` - Cycle through recipes in order
- `random` - Random selection from recipe list
- `same` - Use same recipe for all slots

**Response:**
```json
{
  "isSucceeded": true,
  "data": true,
  "messages": {
    "Success": "Successfully filled 3 meal slots"
  }
}
```

---

## 📝 Manual Meal Plan Management

### Create Meal Plan
**POST** `/api/mealplans`

Create a new empty meal plan for manual meal entry.

**Request Body:**
```json
{
  "name": "My Custom Meal Plan",
  "startDate": "2024-01-01",
  "endDate": "2024-01-07",
  "planType": "Personal",
  "familyId": null,
  "notes": "Custom meal plan notes"
}
```

**Plan Types:**
- `Personal` - Individual meal plan
- `Family` - Family shared meal plan
- `Weekly` - Weekly meal plan
- `Monthly` - Monthly meal plan

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 456,
    "name": "My Custom Meal Plan",
    "startDate": "2024-01-01",
    "endDate": "2024-01-07",
    "planType": "Personal",
    "notes": "Custom meal plan notes",
    "totalMeals": 0,
    "completedMeals": 0,
    "createdAt": "2024-01-01T10:00:00Z",
    "mealEntries": []
  },
  "messages": {
    "Success": "Meal plan created successfully"
  }
}
```

---

### Add Meal Entry
**POST** `/api/mealplans/{mealPlanId}/entries`

Add a specific meal entry to an existing meal plan.

**Path Parameters:**
- `mealPlanId` - ID of the meal plan

**Request Body:**
```json
{
  "mealDate": "2024-01-01",
  "mealType": "Breakfast",
  "recipeId": 123,
  "servings": 1,
  "notes": "Extra crispy"
}
```

**Meal Entry Options (choose one):**
- `recipeId` - Use existing recipe
- `productId` - Use food product
- `customMealName` - Custom meal name

**Meal Types:**
- `Breakfast`
- `Lunch` 
- `Dinner`
- `Snack`

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 789,
    "mealDate": "2024-01-01",
    "mealType": "Breakfast",
    "mealName": "Mediterranean Scrambled Eggs",
    "servings": 1,
    "notes": "Extra crispy",
    "recipeId": 123,
    "recipe": {
      "id": 123,
      "name": "Mediterranean Scrambled Eggs",
      "cookingTime": 15
    },
    "isCompleted": false
  },
  "messages": {
    "Success": "Meal entry added successfully"
  }
}
```

---

## 🔍 Query Endpoints

### Get User Meal Plans
**GET** `/api/mealplans`

Retrieve paginated list of user's meal plans.

**Query Parameters:**
- `pageNumber` (optional) - Page number (default: 1)
- `pageSize` (optional) - Items per page (default: 10)

**Example Request:**
```
GET /api/mealplans?pageNumber=1&pageSize=5
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "items": [
      {
        "id": 456,
        "name": "Weekly Meal Plan",
        "startDate": "2024-01-01",
        "endDate": "2024-01-07",
        "planType": "Weekly",
        "totalMeals": 21,
        "completedMeals": 5,
        "createdAt": "2024-01-01T10:00:00Z"
      }
    ],
    "totalCount": 12,
    "pageNumber": 1,
    "pageSize": 5,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "messages": {
    "Success": "Meal plans retrieved successfully"
  }
}
```

---

### Get Meal Plan by ID
**GET** `/api/mealplans/{id}`

Retrieve detailed information about a specific meal plan.

**Path Parameters:**
- `id` - Meal plan ID

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 456,
    "name": "Weekly Meal Plan",
    "startDate": "2024-01-01",
    "endDate": "2024-01-07",
    "planType": "Weekly",
    "notes": "My weekly meal plan",
    "totalMeals": 21,
    "completedMeals": 5,
    "createdAt": "2024-01-01T10:00:00Z",
    "mealEntries": [
      {
        "id": 789,
        "mealDate": "2024-01-01",
        "mealType": "Breakfast",
        "mealName": "Mediterranean Scrambled Eggs",
        "servings": 1,
        "recipeId": 123,
        "recipe": {
          "id": 123,
          "name": "Mediterranean Scrambled Eggs",
          "cookingTime": 15,
          "difficulty": "Easy"
        },
        "isCompleted": false
      }
    ]
  },
  "messages": {
    "Success": "Meal plan retrieved successfully"
  }
}
```

**Status Codes:**
- `200 OK` - Meal plan found
- `404 Not Found` - Meal plan not found
- `400 Bad Request` - Invalid meal plan ID

---

## ✏️ Update & Delete Operations

### Update Meal Plan
**PUT** `/api/mealplans/{id}`

Update meal plan basic information.

**Path Parameters:**
- `id` - Meal plan ID

**Request Body:**
```json
{
  "name": "Updated Meal Plan Name",
  "startDate": "2024-01-01",
  "endDate": "2024-01-14",
  "planType": "Bi-weekly",
  "notes": "Updated notes"
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 456,
    "name": "Updated Meal Plan Name",
    "startDate": "2024-01-01",
    "endDate": "2024-01-14",
    "planType": "Bi-weekly",
    "notes": "Updated notes",
    "totalMeals": 42,
    "completedMeals": 5,
    "createdAt": "2024-01-01T10:00:00Z",
    "mealEntries": []
  },
  "messages": {
    "Success": "Meal plan updated successfully"
  }
}
```

---

### Delete Meal Plan
**DELETE** `/api/mealplans/{id}`

Permanently delete a meal plan and all associated meal entries.

**Path Parameters:**
- `id` - Meal plan ID

**Response:**
```json
{
  "isSucceeded": true,
  "data": true,
  "messages": {
    "Success": "Meal plan deleted successfully"
  }
}
```

**Status Codes:**
- `200 OK` - Meal plan deleted successfully
- `404 Not Found` - Meal plan not found
- `403 Forbidden` - Not authorized to delete this meal plan

---

## 📊 Data Models

### MealPlanDto
```typescript
interface MealPlanDto {
  id: number;
  name: string;
  startDate: string; // ISO date format
  endDate: string;   // ISO date format
  planType: string;  // "Personal" | "Family" | "Weekly" | "Monthly"
  notes: string;
  totalMeals: number;
  completedMeals: number;
  createdAt: string; // ISO datetime format
  mealEntries: MealEntryDto[];
}
```

### MealEntryDto
```typescript
interface MealEntryDto {
  id: number;
  mealDate: string;    // ISO date format
  mealType: string;    // "Breakfast" | "Lunch" | "Dinner" | "Snack"
  mealName: string;    // Display name of the meal
  servings: number;
  notes: string;
  recipeId?: number;   // Optional recipe reference
  productId?: number;  // Optional product reference
  recipe?: RecipeDto;  // Populated recipe data
  isCompleted: boolean;
}
```

### MealPlanPreferencesDto
```typescript
interface MealPlanPreferencesDto {
  cuisineTypes: string[];        // e.g., ["Italian", "Asian", "Mexican"]
  maxCookingTime?: number;       // in minutes
  budgetRange: string;           // "low" | "medium" | "high"
  dietaryGoals: string[];        // e.g., ["balanced", "protein-rich", "low-carb"]
  mealComplexity: string;        // "simple" | "moderate" | "complex"
  preferredMealTypes: string[];  // e.g., ["breakfast", "lunch", "dinner"]
  includeLeftovers: boolean;
  varietyMode: boolean;          // Ensure meal variety
}
```

---

## ⚠️ Error Handling

### Standard Error Response
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "Error": "Detailed error message",
    "ValidationField": "Field-specific error message"
  }
}
```

### Common Error Codes

| Status Code | Error Type | Description |
|-------------|------------|-------------|
| `400` | Bad Request | Invalid request data or validation errors |
| `401` | Unauthorized | Missing or invalid authentication token |
| `403` | Forbidden | Insufficient permissions for the operation |
| `404` | Not Found | Requested resource not found |
| `409` | Conflict | Resource conflict (e.g., duplicate meal plan) |
| `500` | Internal Server Error | Unexpected server error |

### Validation Rules

#### Meal Plan Creation
- **Name**: Required, 1-100 characters
- **Start Date**: Required, must be valid date
- **End Date**: Required, must be after start date
- **Plan Type**: Required, must be valid enum value

#### Meal Entry Creation
- **Meal Date**: Required, must be within meal plan date range
- **Meal Type**: Required, must be valid enum value
- **Meal Source**: Exactly one of `recipeId`, `productId`, or `customMealName` required
- **Servings**: Must be positive number if provided

---

## ⚡ Performance & Caching

### Caching Strategy
The API implements multi-layer caching to optimize performance:

| Data Type | Cache Duration | Cache Strategy |
|-----------|---------------|----------------|
| User Allergies | 2 hours | Memory + Redis |
| Recipe Filtering | 30 minutes | Redis |
| Individual Meal Plans | 15 minutes | Memory + Redis |
| User Meal Plan Lists | 10 minutes | Redis |
| Templates | 6 hours | Memory + Redis |

### Cache Invalidation
- **Create Operations**: Invalidate user meal plan lists
- **Update Operations**: Invalidate specific meal plan + user lists
- **Delete Operations**: Invalidate specific meal plan + user lists
- **Meal Entry Changes**: Invalidate affected meal plan + user lists

### Performance Tips
1. **Pagination**: Use appropriate page sizes (10-20 items recommended)
2. **Filtering**: Leverage cached recipe recommendations
3. **Bulk Operations**: Use bulk-fill for multiple meal entries
4. **Templates**: Use predefined templates for faster meal plan creation

---

## 🚀 Usage Examples

### Complete Meal Plan Workflow

```javascript
// 1. Create a smart meal plan
const smartPlan = await fetch('/api/mealplans/generate-smart', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer YOUR_TOKEN',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    name: "Weekly Meal Plan",
    startDate: "2024-01-01",
    endDate: "2024-01-07",
    planType: "Weekly",
    preferences: {
      cuisineTypes: ["Italian", "Mediterranean"],
      maxCookingTime: 45,
      budgetRange: "medium",
      dietaryGoals: ["balanced"],
      mealComplexity: "moderate",
      varietyMode: true
    }
  })
});

// 2. Get meal plan details
const mealPlan = await fetch(`/api/mealplans/${smartPlan.data.id}`, {
  headers: { 'Authorization': 'Bearer YOUR_TOKEN' }
});

// 3. Add custom meal entry
const customMeal = await fetch(`/api/mealplans/${smartPlan.data.id}/entries`, {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer YOUR_TOKEN',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    mealDate: "2024-01-01",
    mealType: "Snack",
    customMealName: "Greek Yogurt with Berries",
    servings: 1
  })
});

// 4. Bulk fill remaining dinners
const bulkFill = await fetch('/api/mealplans/bulk-fill', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer YOUR_TOKEN',
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    mealPlanId: smartPlan.data.id,
    mealType: "dinner",
    fillPattern: "rotate",
    recipeIds: [123, 456, 789],
    targetDates: ["2024-01-05", "2024-01-06", "2024-01-07"]
  })
});
```

---

## 📞 Support & Feedback

For technical support or API feedback, please contact:
- **Email**: api-support@drhan.com
- **Documentation**: [API Documentation Portal](https://docs.drhan.com)
- **Status Page**: [API Status](https://status.drhan.com)

---

*Last Updated: January 2024*
*API Version: v1.0* 