# Data Management API Documentation

## Overview

This API provides comprehensive data management capabilities for the DrHan system, including data seeding, cleaning, resetting, monitoring, and health checking operations.

**Base URL:** `https://your-domain.com/api/datamanagement`

**Authentication:** Bearer Token (JWT) with **Admin Role Required** for all endpoints

**Authorization:** All endpoints require **Admin** role - only administrators can access data management operations.

---

## Response Format

All endpoints return responses wrapped in a consistent `AppResponse<T>` format:

### Success Response Structure:
```json
{
  "isSucceeded": true,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {},
  "data": {
    // Actual response data here
  },
  "pagination": null
}
```

### Error Response Structure:
```json
{
  "isSucceeded": false,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {
    "ErrorCategory": ["Error message"],
    "Details": ["Detailed error information"]
  },
  "data": null,
  "pagination": null
}
```

**Response Fields:**
- `isSucceeded`: Boolean indicating operation success
- `timestamp`: UTC timestamp when response was generated
- `messages`: Dictionary of categorized messages (empty for success, error details for failures)
- `data`: The actual response data (null on error)
- `pagination`: Pagination information (null for non-paginated responses)

---

## Security Requirements

🔒 **All endpoints require:**
- Valid JWT Bearer Token
- User must have **Admin** role
- Proper authorization headers

**Headers Required:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json (for POST/PUT requests)
```

---

## Data Seeding Endpoints

### 1. Seed All Data

**Endpoint:** `POST /seed/all`

**Description:** Seeds all data including allergens, ingredients, relationships, users, and roles

**Request:** No body required

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {},
  "data": {
    "message": "All data seeded successfully",
    "operation": "SeedAll",
    "recordsAffected": 2500,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "pagination": null
}
```

**Error Response (500):**
```json
{
  "isSucceeded": false,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {
    "SeedOperation": ["Failed to seed all data"],
    "Details": ["Database connection timeout"]
  },
  "data": null,
  "pagination": null
}
```

---

### 2. Seed Allergen Data

**Endpoint:** `POST /seed/allergens`

**Description:** Seeds only allergen data and related information

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Allergen data seeded successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to seed allergen data",
  "details": "Specific error details..."
}
```

---

### 3. Seed Ingredient Data

**Endpoint:** `POST /seed/ingredients`

**Description:** Seeds only ingredient data and related information

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Ingredient data seeded successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to seed ingredient data",
  "details": "Specific error details..."
}
```

---

### 4. Seed Users and Roles

**Endpoint:** `POST /seed/users`

**Description:** Seeds user accounts and role assignments

**Request:** No body required

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {},
  "data": {
    "message": "Users and roles seeded successfully",
    "userStatistics": {
      "totalUsers": 150,
      "adminUsers": 5,
      "customerUsers": 145,
      "enabledUsers": 148,
      "disabledUsers": 2,
      "confirmedEmails": 140,
      "unconfirmedEmails": 10,
      "timestamp": "2024-01-15T10:30:00Z"
    },
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "pagination": null
}
```

**Error Response (500):**
```json
{
  "error": "Failed to seed users and roles",
  "details": "Specific error details..."
}
```

---

### 5. Seed Food Data Only

**Endpoint:** `POST /seed/food`

**Description:** Seeds allergens and ingredients but excludes users

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Food data seeded successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to seed food data",
  "details": "Specific error details..."
}
```

---

## Data Cleaning Endpoints

### 6. Clean All Data

**Endpoint:** `DELETE /clean/all`

**Description:** Removes all seeded data including users (⚠️ Use with extreme caution)

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "All data cleaned successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to clean data",
  "details": "Specific error details..."
}
```

⚠️ **Warning:** This operation removes all data from the database. Ensure you have backups before proceeding.

---

### 7. Clean Food Data

**Endpoint:** `DELETE /clean/food`

**Description:** Removes only food-related data (allergens, ingredients) but preserves users

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Food data cleaned successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to clean food data",
  "details": "Specific error details..."
}
```

---

### 8. Clean Users and Roles

**Endpoint:** `DELETE /clean/users`

**Description:** Removes seeded users and roles (keeps admin user)

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Users and roles cleaned successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to clean users and roles",
  "details": "Specific error details..."
}
```

---

## Data Reset Endpoints

### 9. Reset All Data

**Endpoint:** `POST /reset/all`

**Description:** Cleans and reseeds all data (complete refresh)

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "All data reset successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to reset all data",
  "details": "Specific error details..."
}
```

---

### 10. Reset Food Data

**Endpoint:** `POST /reset/food`

**Description:** Cleans and reseeds only food data (preserves users)

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Food data reset successfully",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to reset food data",
  "details": "Specific error details..."
}
```

---

### 11. Reset Users and Roles

**Endpoint:** `POST /reset/users`

**Description:** Cleans and reseeds users and roles

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Users and roles reset successfully",
  "userStatistics": {
    "totalUsers": 150,
    "adminUsers": 5,
    "customerUsers": 145,
    "enabledUsers": 148,
    "disabledUsers": 2,
    "confirmedEmails": 140,
    "unconfirmedEmails": 10,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to reset users and roles",
  "details": "Specific error details..."
}
```

---

## Monitoring and Statistics Endpoints

### 12. Get Database Statistics

**Endpoint:** `GET /statistics`

**Description:** Retrieves comprehensive database statistics

**Request:** No body required

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "timestamp": "2024-01-15T10:30:00Z",
  "messages": {},
  "data": {
    "crossReactivityGroupsCount": 25,
    "allergensCount": 150,
    "allergenNamesCount": 450,
    "ingredientsCount": 2500,
    "ingredientNamesCount": 7500,
    "allergenIngredientRelationsCount": 1200,
    "totalRecords": 11825,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "pagination": null
}
```

**Error Response (500):**
```json
{
  "error": "Failed to get statistics",
  "details": "Specific error details..."
}
```

---

### 13. Get User Statistics

**Endpoint:** `GET /statistics/users`

**Description:** Retrieves detailed user account statistics

**Request:** No body required

**Success Response (200):**
```json
{
  "totalUsers": 150,
  "adminUsers": 5,
  "customerUsers": 145,
  "enabledUsers": 148,
  "disabledUsers": 2,
  "confirmedEmails": 140,
  "unconfirmedEmails": 10,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to get user statistics",
  "details": "Specific error details..."
}
```

---

### 14. Health Check

**Endpoint:** `GET /health`

**Description:** Performs comprehensive system health check

**Request:** No body required

**Success Response (200):**
```json
{
  "isHealthy": true,
  "canConnectToDatabase": true,
  "hasData": true,
  "hasCompleteData": true,
  "statistics": {
    "crossReactivityGroupsCount": 25,
    "allergensCount": 150,
    "allergenNamesCount": 450,
    "ingredientsCount": 2500,
    "ingredientNamesCount": 7500,
    "allergenIngredientRelationsCount": 1200,
    "totalRecords": 11825,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "jsonValidation": {
    "isValid": true,
    "errors": [],
    "warnings": [],
    "filesChecked": 5,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "error": null,
  "checkedAt": "2024-01-15T10:30:00Z"
}
```

**Unhealthy Response (200):**
```json
{
  "isHealthy": false,
  "canConnectToDatabase": false,
  "hasData": false,
  "hasCompleteData": false,
  "statistics": null,
  "jsonValidation": null,
  "error": "Database connection failed",
  "checkedAt": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Health check failed",
  "details": "Specific error details..."
}
```

---

### 15. Validate JSON Data

**Endpoint:** `GET /validate`

**Description:** Validates integrity of JSON data files

**Request:** No body required

**Success Response (200):**
```json
{
  "isValid": true,
  "errors": [],
  "warnings": [
    "Warning: Some ingredient names are duplicated"
  ],
  "filesChecked": 5,
  "validFiles": 5,
  "invalidFiles": 0,
  "details": {
    "allergens.json": "Valid",
    "ingredients.json": "Valid",
    "crossReactivity.json": "Valid",
    "allergenNames.json": "Valid",
    "ingredientNames.json": "Valid"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Invalid Data Response (200):**
```json
{
  "isValid": false,
  "errors": [
    "allergens.json: Invalid JSON format",
    "ingredients.json: Missing required field 'name'"
  ],
  "warnings": [],
  "filesChecked": 5,
  "validFiles": 3,
  "invalidFiles": 2,
  "details": {
    "allergens.json": "Invalid JSON format",
    "ingredients.json": "Missing required field",
    "crossReactivity.json": "Valid",
    "allergenNames.json": "Valid",
    "ingredientNames.json": "Valid"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "JSON validation failed",
  "details": "Specific error details..."
}
```

---

## Utility Endpoints

### 16. Ensure Data

**Endpoint:** `POST /ensure`

**Description:** Intelligently seeds data only if database is empty or incomplete

**Request:** No body required

**Success Response (200):**
```json
{
  "message": "Data ensured successfully",
  "statistics": {
    "crossReactivityGroupsCount": 25,
    "allergensCount": 150,
    "allergenNamesCount": 450,
    "ingredientsCount": 2500,
    "ingredientNamesCount": 7500,
    "allergenIngredientRelationsCount": 1200,
    "totalRecords": 11825,
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (500):**
```json
{
  "error": "Failed to ensure data",
  "details": "Specific error details..."
}
```

**Logic:**
- If database is completely empty → Seeds all data
- If users exist but no food data → Seeds only food data  
- If food data exists but no users → Seeds only users
- If both exist → No action taken

---

## Error Codes

| Status Code | Description | When It Occurs |
|-------------|-------------|----------------|
| 200 | Success | Operation completed successfully |
| 401 | Unauthorized | Missing or invalid JWT token |
| 403 | Forbidden | User lacks Admin role |
| 500 | Internal Server Error | Database error, file system error, or other server issues |

---

## Common Error Response Format

All error responses follow this structure:

```json
{
  "error": "Brief error description",
  "details": "Detailed error message with technical information"
}
```

---

## Data Types and Relationships

### Database Entities Managed:
- **Cross-Reactivity Groups**: Allergen groupings for related allergens
- **Allergens**: Individual allergen records  
- **Allergen Names**: Multi-language allergen names
- **Ingredients**: Food ingredient records
- **Ingredient Names**: Multi-language ingredient names
- **Relations**: Allergen-ingredient relationships
- **Users**: User accounts with roles
- **Roles**: Admin and Customer roles

### Data Relationships:
```
Cross-Reactivity Groups
    ↓
Allergens ←→ Ingredients (Many-to-Many)
    ↓              ↓
Allergen Names  Ingredient Names
    ↓
Users (Admin/Customer roles)
```

---

## Usage Examples

### Development Workflow:
```bash
# 1. Check system health
GET /api/datamanagement/health

# 2. If unhealthy, ensure data exists
POST /api/datamanagement/ensure

# 3. Get current statistics
GET /api/datamanagement/statistics

# 4. If needed, reset specific data
POST /api/datamanagement/reset/food
```

### Production Maintenance:
```bash
# 1. Validate data integrity
GET /api/datamanagement/validate

# 2. Check database statistics
GET /api/datamanagement/statistics

# 3. Monitor user accounts
GET /api/datamanagement/statistics/users

# 4. Health check for monitoring
GET /api/datamanagement/health
```

### Emergency Reset:
```bash
# Complete system reset (use with caution)
POST /api/datamanagement/reset/all

# Or partial reset
POST /api/datamanagement/reset/food
```

---

## Best Practices

### 🔒 Security
- Always verify user has Admin role before data operations
- Log all data management operations for audit trails
- Use caution with delete/reset operations in production
- Implement proper backup procedures before major operations

### 📊 Monitoring  
- Regularly check `/health` endpoint for system status
- Monitor `/statistics` for data growth and integrity
- Use `/validate` to ensure JSON data file integrity
- Set up alerts for failed operations

### 🔄 Data Management
- Use `/ensure` for safe, conditional data seeding
- Prefer partial resets (`/reset/food`, `/reset/users`) over full resets
- Always validate data after seeding operations
- Test operations in development before production use

---

## Configuration

Data management operations can be configured in `appsettings.json`:

```json
{
  "DatabaseSettings": {
    "AutoMigrate": false,
    "AutoSeed": false
  },
  "ClearAndReseedData": false
}
```

**Settings:**
- `AutoMigrate`: Automatically run database migrations on startup
- `AutoSeed`: Automatically seed data on application startup  
- `ClearAndReseedData`: Clear and reseed all data on startup (development only)

---

**Note:** All endpoints require Admin authentication and follow RESTful conventions. Operations are logged for audit purposes and include comprehensive error handling. 