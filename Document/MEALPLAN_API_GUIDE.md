# MealPlan API Guide

## Overview

This guide covers the DrHan MealPlan API endpoints which provide comprehensive meal planning functionality including smart AI-powered meal generation, manual meal plan management, recipe recommendations, and bulk operations.

**Base URL:** `/api/mealplans`  
**Authentication:** Required (JWT Bearer Token)  
**Features:** Smart AI Generation, Manual Planning, Recipe Recommendations, Bulk Operations

---

## Table of Contents

1. [Authentication](#authentication)
2. [Smart Generation Endpoints](#smart-generation-endpoints)
3. [Manual Meal Plan Management](#manual-meal-plan-management)
4. [Query Endpoints](#query-endpoints)
5. [Update & Delete Operations](#update--delete-operations)
6. [Data Models](#data-models)
7. [Usage Examples](#usage-examples)
8. [Error Handling](#error-handling)

---

## Authentication

All MealPlan endpoints require JWT authentication. Include the bearer token in the Authorization header:

```
Authorization: Bearer YOUR_JWT_TOKEN
```

The API automatically extracts the current user ID from the authentication context.

---

## Smart Generation Endpoints

### 1. Generate Smart Meal Plan

**🎯 Create a complete meal plan with AI-powered recipe selection**

```http
POST /api/mealplans/generate-smart
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "name": "Weekly Meal Plan",
  "startDate": "2024-01-15",
  "endDate": "2024-01-21",
  "planType": "Personal",
  "familyId": null,
  "preferences": {
    "cuisineTypes": ["Italian", "Asian"],
    "maxCookingTime": 45,
    "budgetRange": "medium",
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
    "id": 123,
    "name": "Weekly Meal Plan",
    "startDate": "2024-01-15",
    "endDate": "2024-01-21",
    "planType": "Personal",
    "notes": "",
    "totalMeals": 21,
    "completedMeals": 0,
    "createdAt": "2024-01-14T10:30:00Z",
    "mealEntries": [
      {
        "id": 456,
        "mealDate": "2024-01-15",
        "mealType": "breakfast",
        "mealName": "Italian Breakfast Scramble",
        "servings": 1.0,
        "notes": "",
        "recipeId": 789,
        "productId": null,
        "recipe": {
          "id": 789,
          "name": "Italian Breakfast Scramble",
          "prepTime": 15,
          "cookTime": 10
        },
        "isCompleted": false
      }
    ]
  },
  "messages": {
    "Success": "Smart meal plan generated successfully"
  }
}
```

### 2. Get Recipe Recommendations

**🔍 Get AI-filtered recipe recommendations based on preferences**

```http
POST /api/mealplans/recommendations?mealType=dinner
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "cuisineTypes": ["Italian", "Mediterranean"],
  "maxCookingTime": 60,
  "budgetRange": "medium",
  "preferredMealTypes": ["dinner"],
  "includeLeftovers": false,
  "varietyMode": true
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [123, 456, 789, 321, 654],
  "messages": {
    "Success": "Found 5 recommended recipes"
  }
}
```

### 3. Get Available Templates

**📋 Retrieve pre-built meal plan templates**

```http
GET /api/mealplans/templates
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Mediterranean Week",
      "description": "7-day Mediterranean diet plan",
      "category": "Healthy",
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

### 4. Get Smart Generation Options

**⚙️ Retrieve available options for smart generation**

```http
GET /api/mealplans/smart-generation/options
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "availableCuisineTypes": ["Italian", "Asian", "Mexican", "Mediterranean"],
    "budgetRangeOptions": ["low", "medium", "high"],
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
      "rotate": "Cycle through recipes in order",
      "random": "Randomly select from available recipes",
      "same": "Use the same recipe for all selected dates"
    }
  },
  "messages": {
    "Success": "Smart generation options retrieved successfully"
  }
}
```

### 5. Bulk Fill Meals

**⚡ Fill multiple meal slots with selected recipes**

```http
POST /api/mealplans/bulk-fill
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "mealPlanId": 123,
  "mealType": "dinner",
  "fillPattern": "rotate",
  "recipeIds": [456, 789, 321],
  "targetDates": ["2024-01-15", "2024-01-16", "2024-01-17"]
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": true,
  "messages": {
    "Success": "Meals filled successfully"
  }
}
```

---

## Manual Meal Plan Management

### 6. Create Empty Meal Plan

**📝 Create a new empty meal plan for manual management**

```http
POST /api/mealplans
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "name": "My Custom Meal Plan",
  "startDate": "2024-01-15",
  "endDate": "2024-01-21",
  "planType": "Personal",
  "familyId": null,
  "notes": "Custom meal plan for weight loss"
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 124,
    "name": "My Custom Meal Plan",
    "startDate": "2024-01-15",
    "endDate": "2024-01-21",
    "planType": "Personal",
    "notes": "Custom meal plan for weight loss",
    "totalMeals": 0,
    "completedMeals": 0,
    "createdAt": "2024-01-14T10:30:00Z",
    "mealEntries": []
  },
  "messages": {
    "Success": "Meal plan created successfully"
  }
}
```

### 7. Add Meal Entry

**➕ Add a meal entry to existing meal plan**

```http
POST /api/mealplans/123/entries
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "mealDate": "2024-01-15",
  "mealType": "breakfast",
  "recipeId": 456,
  "productId": null,
  "customMealName": null,
  "servings": 1.0,
  "notes": "Extra protein"
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 789,
    "mealDate": "2024-01-15",
    "mealType": "breakfast",
    "mealName": "Italian Breakfast Scramble",
    "servings": 1.0,
    "notes": "Extra protein",
    "recipeId": 456,
    "productId": null,
    "recipe": {
      "id": 456,
      "name": "Italian Breakfast Scramble",
      "prepTime": 15,
      "cookTime": 10
    },
    "isCompleted": false
  },
  "messages": {
    "Success": "Meal entry added successfully"
  }
}
```

### 8. Generate Smart Meals in Existing Plan

**🎯 Add AI-generated meals to an existing meal plan**

```http
POST /api/mealplans/123/generate-smart-meals
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "preferences": {
    "cuisineTypes": ["Asian"],
    "maxCookingTime": 30,
    "budgetRange": "medium",
    "preferredMealTypes": ["lunch", "dinner"],
    "includeLeftovers": true,
    "varietyMode": true
  },
  "targetDates": ["2024-01-16", "2024-01-17"],
  "mealTypes": ["lunch", "dinner"],
  "replaceExisting": false,
  "preserveFavorites": true
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 123,
    "name": "My Custom Meal Plan",
    "totalMeals": 8,
    "completedMeals": 0,
    "mealEntries": [
      // Updated meal entries including newly generated ones
    ]
  },
  "messages": {
    "Success": "Smart meals generated successfully"
  }
}
```

---

## Query Endpoints

### 9. Get User's Meal Plans

**📄 Retrieve user's meal plans with pagination**

```http
GET /api/mealplans?pageNumber=1&pageSize=10
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "items": [
      {
        "id": 123,
        "name": "Weekly Meal Plan",
        "startDate": "2024-01-15",
        "endDate": "2024-01-21",
        "planType": "Personal",
        "totalMeals": 21,
        "completedMeals": 5,
        "createdAt": "2024-01-14T10:30:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "messages": {
    "Success": "Meal plans retrieved successfully"
  }
}
```

### 10. Get Specific Meal Plan

**🔍 Get detailed meal plan with all entries**

```http
GET /api/mealplans/123
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 123,
    "name": "Weekly Meal Plan",
    "startDate": "2024-01-15",
    "endDate": "2024-01-21",
    "planType": "Personal",
    "notes": "",
    "totalMeals": 21,
    "completedMeals": 5,
    "createdAt": "2024-01-14T10:30:00Z",
    "mealEntries": [
      {
        "id": 456,
        "mealDate": "2024-01-15",
        "mealType": "breakfast",
        "mealName": "Italian Breakfast Scramble",
        "servings": 1.0,
        "notes": "",
        "recipeId": 789,
        "productId": null,
        "recipe": {
          "id": 789,
          "name": "Italian Breakfast Scramble",
          "prepTime": 15,
          "cookTime": 10,
          "description": "Delicious scrambled eggs with Italian herbs"
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

---

## Update & Delete Operations

### 11. Update Meal Plan

**✏️ Update meal plan basic information**

```http
PUT /api/mealplans/123
Content-Type: application/json
Authorization: Bearer YOUR_JWT_TOKEN
```

**Request Body:**
```json
{
  "name": "Updated Weekly Plan",
  "startDate": "2024-01-15",
  "endDate": "2024-01-22",
  "planType": "Personal",
  "notes": "Extended by one day"
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 123,
    "name": "Updated Weekly Plan",
    "startDate": "2024-01-15",
    "endDate": "2024-01-22",
    "planType": "Personal",
    "notes": "Extended by one day",
    "totalMeals": 24,
    "completedMeals": 5,
    "mealEntries": []
  },
  "messages": {
    "Success": "Meal plan updated successfully"
  }
}
```

### 12. Delete Meal Plan

**🗑️ Delete a meal plan and all its entries**

```http
DELETE /api/mealplans/123
Authorization: Bearer YOUR_JWT_TOKEN
```

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

---

## Data Models

### Core Models

#### MealPlanDto
```json
{
  "id": 123,
  "name": "string",
  "startDate": "2024-01-15",
  "endDate": "2024-01-21",
  "planType": "Personal|Family|Weekly|Monthly",
  "notes": "string",
  "totalMeals": 21,
  "completedMeals": 5,
  "createdAt": "2024-01-14T10:30:00Z",
  "mealEntries": []
}
```

#### MealEntryDto
```json
{
  "id": 456,
  "mealDate": "2024-01-15",
  "mealType": "breakfast|lunch|dinner|snack",
  "mealName": "string",
  "servings": 1.0,
  "notes": "string",
  "recipeId": 789,
  "productId": null,
  "recipe": {},
  "isCompleted": false
}
```

#### MealPlanPreferencesDto
```json
{
  "cuisineTypes": ["Italian", "Asian"],
  "maxCookingTime": 45,
  "budgetRange": "low|medium|high",
  "preferredMealTypes": ["breakfast", "lunch", "dinner"],
  "includeLeftovers": true,
  "varietyMode": true
}
```

### Request Models

#### CreateMealPlanDto
```json
{
  "name": "string",
  "startDate": "2024-01-15",
  "endDate": "2024-01-21",
  "planType": "Personal|Family|Weekly|Monthly",
  "familyId": null,
  "notes": "string"
}
```

#### AddMealEntryDto
```json
{
  "mealPlanId": 123,
  "mealDate": "2024-01-15",
  "mealType": "breakfast|lunch|dinner|snack",
  "recipeId": 456,
  "productId": null,
  "customMealName": "string",
  "servings": 1.0,
  "notes": "string"
}
```

#### BulkFillMealsDto
```json
{
  "mealPlanId": 123,
  "mealType": "breakfast|lunch|dinner|snack|all",
  "fillPattern": "rotate|random|same",
  "recipeIds": [456, 789, 321],
  "targetDates": ["2024-01-15", "2024-01-16"]
}
```

---

## Usage Examples

### Complete Workflow: Smart Meal Planning

```javascript
// 1. Get available options
const options = await fetch('/api/mealplans/smart-generation/options', {
  headers: { 'Authorization': 'Bearer ' + token }
});

// 2. Generate smart meal plan
const mealPlan = await fetch('/api/mealplans/generate-smart', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    name: "My Weekly Plan",
    startDate: "2024-01-15",
    endDate: "2024-01-21",
    planType: "Personal",
    preferences: {
      cuisineTypes: ["Italian", "Asian"],
      maxCookingTime: 45,
      budgetRange: "medium",
      preferredMealTypes: ["breakfast", "lunch", "dinner"],
      includeLeftovers: true,
      varietyMode: true
    }
  })
});

// 3. Get recommendations for specific meal type
const recommendations = await fetch('/api/mealplans/recommendations?mealType=dinner', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    cuisineTypes: ["Italian"],
    maxCookingTime: 30,
    budgetRange: "medium"
  })
});
```

### Manual Meal Planning Workflow

```javascript
// 1. Create empty meal plan
const mealPlan = await fetch('/api/mealplans', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    name: "Custom Plan",
    startDate: "2024-01-15",
    endDate: "2024-01-21",
    planType: "Personal",
    notes: "My custom meal plan"
  })
});

// 2. Add individual meal entries
const mealEntry = await fetch(`/api/mealplans/${mealPlan.data.id}/entries`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    mealDate: "2024-01-15",
    mealType: "breakfast",
    recipeId: 456,
    servings: 1.0,
    notes: "Extra protein"
  })
});

// 3. Bulk fill remaining meals
const bulkFill = await fetch('/api/mealplans/bulk-fill', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    mealPlanId: mealPlan.data.id,
    mealType: "lunch",
    fillPattern: "rotate",
    recipeIds: [789, 321, 654],
    targetDates: ["2024-01-16", "2024-01-17", "2024-01-18"]
  })
});
```

---

## Error Handling

### Common Error Responses

#### Validation Error (400)
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "StartDate": "Start date cannot be in the past",
    "Name": "Name is required"
  }
}
```

#### Unauthorized (401)
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "Error": "Unauthorized access"
  }
}
```

#### Not Found (404)
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "NotFound": "Meal plan not found"
  }
}
```

#### Server Error (500)
```json
{
  "isSucceeded": false,
  "data": null,
  "messages": {
    "Error": "An unexpected error occurred"
  }
}
```

### Error Codes Reference

| Error Type | HTTP Status | Description |
|------------|-------------|-------------|
| `Validation` | 400 | Invalid request data |
| `NotFound` | 404 | Resource not found |
| `Unauthorized` | 401 | Authentication required |
| `Forbidden` | 403 | Insufficient permissions |
| `NoRecipesFound` | 400 | No recipes match criteria |
| `Error` | 500 | Internal server error |

---

## Best Practices

### 1. Smart Generation Tips
- **Use variety mode** for better meal diversity
- **Set realistic cooking times** based on your schedule
- **Include multiple cuisine types** for variety
- **Enable leftovers** to reduce food waste

### 2. Performance Optimization
- **Use pagination** for large meal plan lists
- **Cache smart generation options** as they change infrequently
- **Batch operations** using bulk-fill when possible

### 3. Error Handling
- **Always check** the `isSucceeded` field in responses
- **Handle NoRecipesFound** errors by adjusting preferences
- **Implement retry logic** for transient errors

### 4. User Experience
- **Show loading states** during smart generation (can take 3-5 seconds)
- **Provide fallback options** when no recipes match criteria
- **Allow preference adjustments** when generation fails

---

## Rate Limits

- **Smart Generation**: 10 requests per minute per user
- **Recommendations**: 20 requests per minute per user
- **General API**: 100 requests per minute per user

Exceed rate limits will result in HTTP 429 responses.

---

## Support

For API support or questions:
- **Email**: api-support@drhan.com
- **Documentation**: [DrHan API Docs](https://docs.drhan.com)
- **Status Page**: [DrHan Status](https://status.drhan.com) 