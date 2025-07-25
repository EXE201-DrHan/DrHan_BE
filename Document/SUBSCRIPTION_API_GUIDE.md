# Subscription and UserSubscription API Guide

## Overview

This guide covers the DrHan API endpoints for subscription management, including both user-facing operations and administrative functions. The API provides two main controllers:

- **UserSubscriptionController** (`/api/usersubscription`) - User subscription management
- **SubscriptionController** (`/api/subscription`) - Subscription plans and admin features

---

## Table of Contents

1. [Authentication](#authentication)
2. [User Subscription Management](#user-subscription-management)
3. [Subscription Plans (Public)](#subscription-plans-public)
4. [Admin Endpoints](#admin-endpoints)
5. [Data Models](#data-models)
6. [Common Scenarios](#common-scenarios)
7. [Error Handling](#error-handling)

---

## Authentication

All endpoints except public plan viewing require JWT authentication. Include the bearer token in the Authorization header:

```
Authorization: Bearer YOUR_JWT_TOKEN
```

Admin endpoints require the `Admin` role.

---

## User Subscription Management

Base URL: `/api/usersubscription`

### 1. Get Subscription Status

Get the current user's subscription status and limits.

**Endpoint:** `GET /api/usersubscription/status`

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "hasActiveSubscription": true,
    "currentSubscription": {
      "id": 123,
      "userId": 456,
      "planId": 2,
      "planName": "Premium Plan",
      "planPrice": 99000.00,
      "currency": "VND",
      "billingCycle": "monthly",
      "startDate": "2024-01-01T00:00:00Z",
      "endDate": "2024-02-01T00:00:00Z",
      "status": "Active",
      "createdAt": "2024-01-01T00:00:00Z",
      "isActive": true,
      "daysRemaining": 15
    },
    "planLimits": {
      "recipe_generation": 100,
      "meal_planning": 50
    },
    "currentUsage": {
      "recipe_generation": 23,
      "meal_planning": 8
    }
  }
}
```

### 2. Create Subscription

Subscribe to a new plan.

**Endpoint:** `POST /api/usersubscription`

**Request Body:**
```json
{
  "planId": 2
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 123,
    "userId": 456,
    "planId": 2,
    "planName": "Premium Plan",
    "planPrice": 99000.00,
    "currency": "VND",
    "billingCycle": "monthly",
    "startDate": "2024-01-01T00:00:00Z",
    "endDate": null,
    "status": "Pending",
    "createdAt": "2024-01-01T00:00:00Z",
    "isActive": false,
    "daysRemaining": null
  }
}
```

**Note:** Subscription will be in `Pending` status until payment is completed.

### 3. Cancel Subscription

Cancel the current subscription.

**Endpoint:** `POST /api/usersubscription/cancel`

**Request Body (Optional):**
```json
{
  "cancellationReason": "Too expensive"
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "message": "Subscription cancelled successfully"
}
```

### 4. Renew Subscription

Renew an expired subscription.

**Endpoint:** `POST /api/usersubscription/renew`

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 124,
    "userId": 456,
    "planId": 2,
    "planName": "Premium Plan",
    "status": "Pending"
  }
}
```

### 5. Upgrade Subscription

Upgrade to a higher-tier plan.

**Endpoint:** `POST /api/usersubscription/upgrade`

**Request Body:**
```json
{
  "newPlanId": 3
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 125,
    "planName": "Enterprise Plan",
    "status": "Active"
  }
}
```

### 6. Check Feature Access

Check if the user can use a specific feature.

**Endpoint:** `GET /api/usersubscription/can-use/{featureName}?limitType=daily`

**Parameters:**
- `featureName`: Name of the feature (e.g., "recipe_generation", "meal_planning")
- `limitType`: "daily" or "monthly" (default: "daily")

**Response:**
```json
{
  "canUse": true,
  "featureName": "recipe_generation",
  "limitType": "daily"
}
```

### 7. Track Feature Usage

Record usage of a feature.

**Endpoint:** `POST /api/usersubscription/track-usage`

**Request Body:**
```json
{
  "featureName": "recipe_generation",
  "count": 1
}
```

**Response:**
```json
{
  "success": true,
  "message": "Usage tracked successfully"
}
```

### 8. Get Usage Count

Get current usage count for a feature.

**Endpoint:** `GET /api/usersubscription/usage/{featureName}?fromDate=2024-01-01`

**Parameters:**
- `featureName`: Name of the feature
- `fromDate`: Optional start date (default: today)

**Response:**
```json
{
  "featureName": "recipe_generation",
  "usageCount": 23,
  "fromDate": "2024-01-01T00:00:00Z"
}
```

### 9. Get Purchase History

Retrieve payment and purchase history.

**Endpoint:** `GET /api/usersubscription/history/purchases`

**Query Parameters:**
```
?fromDate=2024-01-01&toDate=2024-01-31&pageNumber=1&pageSize=10
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "amount": 99000.00,
      "currency": "VND",
      "transactionId": "TXN_123456",
      "paymentStatus": "Completed",
      "paymentMethod": "CreditCard",
      "paymentDate": "2024-01-01T10:30:00Z",
      "failureReason": null,
      "planName": "Premium Plan",
      "billingCycle": "monthly"
    }
  ]
}
```

### 10. Get Usage History

Retrieve feature usage history.

**Endpoint:** `GET /api/usersubscription/history/usage`

**Query Parameters:**
```
?featureType=recipe_generation&fromDate=2024-01-01&pageNumber=1&pageSize=10
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "featureType": "recipe_generation",
      "usageCount": 5,
      "usageDate": "2024-01-01T10:30:00Z",
      "resourceUsed": "AI Recipe Generator",
      "planName": "Premium Plan"
    }
  ]
}
```

### 11. Get Subscription History

Retrieve subscription timeline.

**Endpoint:** `GET /api/usersubscription/history/subscriptions`

**Query Parameters:**
```
?fromDate=2024-01-01&toDate=2024-01-31&pageNumber=1&pageSize=10
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 123,
      "planName": "Premium Plan",
      "planPrice": 99000.00,
      "currency": "VND",
      "billingCycle": "monthly",
      "startDate": "2024-01-01T00:00:00Z",
      "endDate": "2024-02-01T00:00:00Z",
      "status": "Active",
      "createdAt": "2024-01-01T00:00:00Z",
      "daysActive": 31
    }
  ]
}
```

---

## Subscription Plans (Public)

Base URL: `/api/subscription`

### 1. Get All Plans

View all active subscription plans.

**Endpoint:** `GET /api/subscription/plans`

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Basic Plan",
      "description": "Perfect for individuals",
      "price": 49000.00,
      "currency": "VND",
      "billingCycle": "monthly",
      "usageQuota": 50,
      "isActive": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "features": [
        {
          "id": 1,
          "planId": 1,
          "featureName": "recipe_generation",
          "description": "Generate AI recipes",
          "isEnabled": true,
          "createdAt": "2024-01-01T00:00:00Z"
        }
      ]
    }
  ]
}
```

### 2. Get Plan by ID

Get details of a specific plan.

**Endpoint:** `GET /api/subscription/plans/{id}`

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 2,
    "name": "Premium Plan",
    "description": "Best for families",
    "price": 99000.00,
    "currency": "VND",
    "billingCycle": "monthly",
    "usageQuota": 100,
    "isActive": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "features": [
      {
        "id": 2,
        "planId": 2,
        "featureName": "recipe_generation",
        "description": "Generate unlimited AI recipes",
        "isEnabled": true,
        "createdAt": "2024-01-01T00:00:00Z"
      },
      {
        "id": 3,
        "planId": 2,
        "featureName": "meal_planning",
        "description": "Smart meal planning",
        "isEnabled": true,
        "createdAt": "2024-01-01T00:00:00Z"
      }
    ]
  }
}
```

---

## Admin Endpoints

Base URL: `/api/subscription/admin`  
**Requires:** Admin role

### 1. Get All Plans (Admin)

View all plans including inactive ones.

**Endpoint:** `GET /api/subscription/admin/plans`

### 2. Create Plan

Create a new subscription plan.

**Endpoint:** `POST /api/subscription/admin/plans`

**Request Body:**
```json
{
  "name": "Enterprise Plan",
  "description": "For large organizations",
  "price": 199000.00,
  "currency": "VND",
  "billingCycle": "monthly",
  "usageQuota": 500,
  "isActive": true,
  "features": [
    {
      "featureName": "recipe_generation",
      "description": "Unlimited AI recipe generation",
      "isEnabled": true
    },
    {
      "featureName": "meal_planning",
      "description": "Advanced meal planning",
      "isEnabled": true
    }
  ]
}
```

### 3. Update Plan

Update an existing plan.

**Endpoint:** `PUT /api/subscription/admin/plans/{id}`

**Request Body:**
```json
{
  "name": "Enterprise Plan Updated",
  "description": "Updated description",
  "price": 229000.00,
  "currency": "VND",
  "billingCycle": "monthly",
  "usageQuota": 600,
  "isActive": true
}
```

### 4. Delete Plan

Delete or deactivate a plan.

**Endpoint:** `DELETE /api/subscription/admin/plans/{id}`

**Response:**
```json
{
  "isSucceeded": true,
  "data": true,
  "message": "Subscription plan deleted successfully"
}
```

**Note:** Plans with active subscriptions will be deactivated instead of deleted.

### 5. Add Feature to Plan

Add a feature to an existing plan.

**Endpoint:** `POST /api/subscription/admin/plans/{planId}/features`

**Request Body:**
```json
{
  "featureName": "allergen_tracking",
  "description": "Advanced allergen tracking",
  "isEnabled": true
}
```

### 6. Remove Feature from Plan

Remove a feature from a plan.

**Endpoint:** `DELETE /api/subscription/admin/plans/{planId}/features/{featureId}`

### 7. Get All User Subscriptions

View all user subscriptions with filtering.

**Endpoint:** `GET /api/subscription/admin/user-subscriptions`

**Query Parameters:**
```
?page=1&pageSize=10&status=Active&planId=2
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "data": [
      {
        "id": 123,
        "userId": 456,
        "userName": "john_doe",
        "userEmail": "john@example.com",
        "planId": 2,
        "planName": "Premium Plan",
        "startDate": "2024-01-01T00:00:00Z",
        "endDate": "2024-02-01T00:00:00Z",
        "status": "Active",
        "createdAt": "2024-01-01T00:00:00Z"
      }
    ],
    "totalCount": 150,
    "page": 1,
    "pageSize": 10,
    "totalPages": 15
  }
}
```

---

## Data Models

### SubscriptionPlanDto
```json
{
  "id": "integer",
  "name": "string",
  "description": "string",
  "price": "decimal",
  "currency": "string",
  "billingCycle": "string",
  "usageQuota": "integer|null",
  "isActive": "boolean",
  "createdAt": "datetime",
  "features": "array of PlanFeatureDto"
}
```

### SubscriptionResponseDto
```json
{
  "id": "integer",
  "userId": "integer",
  "planId": "integer",
  "planName": "string",
  "planPrice": "decimal",
  "currency": "string",
  "billingCycle": "string",
  "startDate": "datetime",
  "endDate": "datetime|null",
  "status": "enum (Pending|Active|Expired|Cancelled)",
  "createdAt": "datetime",
  "isActive": "boolean",
  "daysRemaining": "integer|null"
}
```

### HistoryFilterDto
```json
{
  "fromDate": "datetime|null",
  "toDate": "datetime|null",
  "historyType": "string|null",
  "pageNumber": "integer (default: 1)",
  "pageSize": "integer (default: 20)"
}
```

---

## Common Scenarios

### 1. Complete Subscription Flow

```bash
# Step 1: View available plans
GET /api/subscription/plans

# Step 2: Create subscription
POST /api/usersubscription
{
  "planId": 2
}

# Step 3: Process payment (separate payment API)
POST /api/payment
{
  "amount": 99000.00,
  "currency": "VND",
  "userSubscriptionId": 123
}

# Step 4: Check status after payment
GET /api/usersubscription/status
```

### 2. Feature Usage Pattern

```bash
# Check if feature is available
GET /api/usersubscription/can-use/recipe_generation?limitType=daily

# Use the feature if available
# ... perform feature action ...

# Track the usage
POST /api/usersubscription/track-usage
{
  "featureName": "recipe_generation",
  "count": 1
}
```

### 3. Subscription Management

```bash
# Check current subscription
GET /api/usersubscription/status

# Upgrade subscription
POST /api/usersubscription/upgrade
{
  "newPlanId": 3
}

# View subscription history
GET /api/usersubscription/history/subscriptions
```

---

## Error Handling

### Common Error Responses

**400 Bad Request:**
```json
{
  "isSucceeded": false,
  "errorType": "validation",
  "message": "Invalid input data"
}
```

**401 Unauthorized:**
```json
{
  "isSucceeded": false,
  "errorType": "unauthorized",
  "message": "User ID not found in token"
}
```

**403 Forbidden:**
```json
{
  "isSucceeded": false,
  "errorType": "forbidden",
  "message": "Admin role required"
}
```

**404 Not Found:**
```json
{
  "isSucceeded": false,
  "errorType": "not_found",
  "message": "Subscription plan not found"
}
```

**500 Internal Server Error:**
```json
{
  "isSucceeded": false,
  "errorType": "error",
  "message": "An error occurred while processing the request"
}
```

### Business Logic Errors

**Already Subscribed:**
```json
{
  "isSucceeded": false,
  "errorType": "business_logic",
  "message": "User already has an active subscription"
}
```

**Feature Limit Exceeded:**
```json
{
  "isSucceeded": false,
  "errorType": "limit_exceeded",
  "message": "Daily limit for recipe_generation exceeded"
}
```

**No Active Subscription:**
```json
{
  "isSucceeded": false,
  "errorType": "no_subscription",
  "message": "No active subscription found"
}
```

---

## Notes

1. **Currency:** All prices are in Vietnamese Dong (VND) by default
2. **Timezones:** All datetime values are in UTC
3. **Pagination:** Default page size is 20, maximum is 100
4. **Feature Names:** Common feature names include:
   - `recipe_generation`
   - `meal_planning`
   - `allergen_tracking`
   - `nutrition_analysis`
5. **Status Values:**
   - `Pending`: Awaiting payment
   - `Active`: Currently active
   - `Expired`: Subscription has expired
   - `Cancelled`: User cancelled subscription

For more information, refer to the API documentation or contact the development team. 