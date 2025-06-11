# DrHan API Documentation

## Authentication
All endpoints require JWT Bearer token authentication unless otherwise specified.

**Header Format:**
```
Authorization: Bearer <your_jwt_token>
```

## Response Format
All API responses follow a consistent format using `AppResponse<T>`:

```json
{
  "isSucceeded": true,
  "data": {},
  "message": null,
  "errors": []
}
```

---

## Allergen Management APIs

### 1. Get All Allergens
**GET** `/api/allergen`

**Description:** Retrieve all allergens in the system.

**Authorization:** Required (any authenticated user)

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Peanuts",
      "category": "Nuts",
      "scientificName": "Arachis hypogaea",
      "description": "Common legume allergen",
      "isFdaMajor": true,
      "isEuMajor": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ],
  "message": null,
  "errors": []
}
```

### 2. Get Allergen by ID
**GET** `/api/allergen/{id}`

**Description:** Retrieve a specific allergen by its ID.

**Authorization:** Required (any authenticated user)

**Parameters:**
- `id` (path, integer): Allergen ID

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 1,
    "name": "Peanuts",
    "category": "Nuts",
    "scientificName": "Arachis hypogaea",
    "description": "Common legume allergen",
    "isFdaMajor": true,
    "isEuMajor": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  },
  "message": null,
  "errors": []
}
```

### 3. Create New Allergen
**POST** `/api/allergen`

**Description:** Create a new allergen (Admin only).

**Authorization:** Required (Admin role)

**Request Body:**
```json
{
  "name": "Sesame",
  "category": "Seeds",
  "scientificName": "Sesamum indicum",
  "description": "Sesame seed allergen",
  "isFdaMajor": true,
  "isEuMajor": false
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 15,
    "name": "Sesame",
    "category": "Seeds",
    "scientificName": "Sesamum indicum",
    "description": "Sesame seed allergen",
    "isFdaMajor": true,
    "isEuMajor": false,
    "createdAt": "2024-01-01T12:00:00Z",
    "updatedAt": null
  },
  "message": null,
  "errors": []
}
```

### 4. Update Allergen
**PUT** `/api/allergen/{id}`

**Description:** Update an existing allergen (Admin only).

**Authorization:** Required (Admin role)

**Parameters:**
- `id` (path, integer): Allergen ID

**Request Body:**
```json
{
  "name": "Updated Allergen Name",
  "category": "Updated Category",
  "scientificName": "Updated Scientific Name",
  "description": "Updated description",
  "isFdaMajor": false,
  "isEuMajor": true
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 1,
    "name": "Updated Allergen Name",
    "category": "Updated Category",
    "scientificName": "Updated Scientific Name",
    "description": "Updated description",
    "isFdaMajor": false,
    "isEuMajor": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T12:00:00Z"
  },
  "message": null,
  "errors": []
}
```

### 5. Delete Allergen
**DELETE** `/api/allergen/{id}`

**Description:** Delete an allergen (Admin only).

**Authorization:** Required (Admin role)

**Parameters:**
- `id` (path, integer): Allergen ID

**Response:** 204 No Content

### 6. Search Allergens
**GET** `/api/allergen/search?searchTerm={term}`

**Description:** Search allergens by name, scientific name, or description.

**Authorization:** Required (any authenticated user)

**Query Parameters:**
- `searchTerm` (string, required): Search term

**Example:** `/api/allergen/search?searchTerm=peanut`

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Peanuts",
      "category": "Nuts",
      "scientificName": "Arachis hypogaea",
      "description": "Common legume allergen",
      "isFdaMajor": true,
      "isEuMajor": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ],
  "message": null,
  "errors": []
}
```

### 7. Get Allergens by Category
**GET** `/api/allergen/category/{category}`

**Description:** Retrieve all allergens in a specific category.

**Authorization:** Required (any authenticated user)

**Parameters:**
- `category` (path, string): Category name

**Example:** `/api/allergen/category/Nuts`

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Peanuts",
      "category": "Nuts",
      "scientificName": "Arachis hypogaea",
      "description": "Common legume allergen",
      "isFdaMajor": true,
      "isEuMajor": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    },
    {
      "id": 2,
      "name": "Tree Nuts",
      "category": "Nuts",
      "scientificName": null,
      "description": "Various tree nuts",
      "isFdaMajor": true,
      "isEuMajor": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": null
    }
  ],
  "message": null,
  "errors": []
}
```

### 8. Get All Allergen Categories
**GET** `/api/allergen/categories`

**Description:** Retrieve all distinct allergen categories.

**Authorization:** Required (any authenticated user)

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    "Dairy",
    "Eggs",
    "Fish",
    "Fruits",
    "Grains",
    "Nuts",
    "Seafood",
    "Seeds",
    "Vegetables"
  ],
  "message": null,
  "errors": []
}
```

### 9. Get Major Allergens
**GET** `/api/allergen/major?isFdaMajor={bool}&isEuMajor={bool}`

**Description:** Retrieve major allergens based on FDA and/or EU classification.

**Authorization:** Required (any authenticated user)

**Query Parameters:**
- `isFdaMajor` (boolean, optional): Filter by FDA major allergen status
- `isEuMajor` (boolean, optional): Filter by EU major allergen status

**Examples:**
- `/api/allergen/major?isFdaMajor=true` - Get FDA major allergens
- `/api/allergen/major?isEuMajor=true` - Get EU major allergens
- `/api/allergen/major?isFdaMajor=true&isEuMajor=true` - Get allergens that are major in both FDA and EU

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "name": "Peanuts",
      "category": "Nuts",
      "scientificName": "Arachis hypogaea",
      "description": "Common legume allergen",
      "isFdaMajor": true,
      "isEuMajor": true,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ],
  "message": null,
  "errors": []
}
```

---

## User Allergy Management APIs

### 1. Get My Allergy Profile
**GET** `/api/userallergy/profile`

**Description:** Get the current user's complete allergy profile.

**Authorization:** Required (any authenticated user)

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "userId": 123,
    "userName": "john.doe",
    "email": "john.doe@example.com",
    "totalAllergies": 3,
    "severeAllergies": 1,
    "allergies": [
      {
        "id": 1,
        "userId": 123,
        "allergenId": 1,
        "allergenName": "Peanuts",
        "severity": "Severe",
        "diagnosisDate": "2020-01-15T00:00:00Z",
        "diagnosedBy": "Dr. Smith",
        "lastReactionDate": "2023-06-10T00:00:00Z",
        "avoidanceNotes": "Avoid all peanut products",
        "outgrown": false,
        "outgrownDate": null,
        "needsVerification": false
      }
    ]
  },
  "message": null,
  "errors": []
}
```

### 2. Get User Allergy Profile (Admin)
**GET** `/api/userallergy/profile/{userId}`

**Description:** Get a specific user's allergy profile (Admin only).

**Authorization:** Required (Admin role)

**Parameters:**
- `userId` (path, integer): User ID

**Response:** Same as above but for the specified user.

### 3. Get My Allergies
**GET** `/api/userallergy`

**Description:** Get the current user's allergies list.

**Authorization:** Required (any authenticated user)

**Response:**
```json
{
  "isSucceeded": true,
  "data": [
    {
      "id": 1,
      "userId": 123,
      "allergenId": 1,
      "allergenName": "Peanuts",
      "severity": "Severe",
      "diagnosisDate": "2020-01-15T00:00:00Z",
      "diagnosedBy": "Dr. Smith",
      "lastReactionDate": "2023-06-10T00:00:00Z",
      "avoidanceNotes": "Avoid all peanut products",
      "outgrown": false,
      "outgrownDate": null,
      "needsVerification": false
    }
  ],
  "message": null,
  "errors": []
}
```

### 4. Add New Allergy
**POST** `/api/userallergy`

**Description:** Add a new allergy to the current user's profile.

**Authorization:** Required (any authenticated user)

**Request Body:**
```json
{
  "allergenId": 1,
  "severity": "Moderate",
  "diagnosisDate": "2024-01-15T00:00:00Z",
  "diagnosedBy": "Dr. Johnson",
  "lastReactionDate": "2024-01-10T00:00:00Z",
  "avoidanceNotes": "Avoid direct contact and ingestion",
  "outgrown": false,
  "outgrownDate": null,
  "needsVerification": false
}
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": {
    "id": 25,
    "userId": 123,
    "allergenId": 1,
    "allergenName": "Peanuts",
    "severity": "Moderate",
    "diagnosisDate": "2024-01-15T00:00:00Z",
    "diagnosedBy": "Dr. Johnson",
    "lastReactionDate": "2024-01-10T00:00:00Z",
    "avoidanceNotes": "Avoid direct contact and ingestion",
    "outgrown": false,
    "outgrownDate": null,
    "needsVerification": false
  },
  "message": null,
  "errors": []
}
```

---

## Error Responses

### Validation Errors
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Name is required",
    "Category must not exceed 50 characters"
  ]
}
```

### Authentication Errors
**401 Unauthorized**
```json
{
  "message": "User ID not found in token"
}
```

### Authorization Errors
**403 Forbidden**
```json
{
  "message": "You can only access your own allergy records"
}
```

### Not Found Errors
**404 Not Found**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Allergen with ID 999 not found",
  "errors": []
}
```

### Server Errors
**500 Internal Server Error**
```json
{
  "message": "An error occurred while processing your request"
}
```

---

## Data Models

### AllergenDto
```json
{
  "id": 1,
  "name": "string (required, max 100 chars)",
  "category": "string (required, max 50 chars)",
  "scientificName": "string (optional, max 100 chars)",
  "description": "string (optional, max 500 chars)",
  "isFdaMajor": "boolean (optional)",
  "isEuMajor": "boolean (optional)",
  "createdAt": "datetime",
  "updatedAt": "datetime (nullable)"
}
```

### UserAllergyDto
```json
{
  "id": 1,
  "userId": 123,
  "allergenId": 1,
  "allergenName": "string",
  "severity": "string (Mild/Moderate/Severe)",
  "diagnosisDate": "datetime (optional)",
  "diagnosedBy": "string (optional)",
  "lastReactionDate": "datetime (optional)",
  "avoidanceNotes": "string (optional)",
  "outgrown": "boolean",
  "outgrownDate": "datetime (optional)",
  "needsVerification": "boolean"
}
```

### UserAllergyProfileDto
```json
{
  "userId": 123,
  "userName": "string",
  "email": "string",
  "totalAllergies": 3,
  "severeAllergies": 1,
  "allergies": ["array of UserAllergyDto"]
}
```

---

## Status Codes

| Code | Description |
|------|-------------|
| 200  | OK - Request successful |
| 201  | Created - Resource created successfully |
| 204  | No Content - Delete successful |
| 400  | Bad Request - Invalid input or validation error |
| 401  | Unauthorized - Authentication required |
| 403  | Forbidden - Insufficient permissions |
| 404  | Not Found - Resource not found |
| 500  | Internal Server Error - Server error |

---

## Rate Limiting
Currently no rate limiting is implemented, but it's recommended for production use.

## Notes
- All datetime fields are in ISO 8601 format (UTC)
- String comparisons for categories and search are case-insensitive
- Admin role is required for allergen management (Create, Update, Delete)
- Users can only manage their own allergy records
- The API follows RESTful conventions and CQRS architecture patterns 