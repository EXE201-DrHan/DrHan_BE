# Authentication API Documentation

## Overview

This API provides comprehensive authentication services including user registration, login, token management, and account security features.

**Base URL:** `https://your-domain.com/api/authentication`

**Authentication:** Bearer Token (JWT) for protected endpoints

---

## Endpoints

### 1. User Registration

**Endpoint:** `POST /register`

**Description:** Register a new user account

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!",
  "fullName": "John Doe",
  "dateOfBirth": "1990-01-15",
  "gender": "Male"
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "userId": 123,
    "email": "user@example.com",
    "fullName": "John Doe",
    "message": "Registration successful. Please check your email to confirm your account."
  },
  "message": "User registered successfully",
  "errors": null
}
```

**Error Response (400):**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Registration failed",
  "errors": {
    "Email": ["User with this email already exists"],
    "Password": ["Password must be at least 6 characters"],
    "Gender": ["Invalid gender value"]
  }
}
```

**Validation Rules:**
- Email: Required, valid email format
- Password: Required, minimum 6 characters
- ConfirmPassword: Required, must match password
- FullName: Required
- DateOfBirth: Required, valid date
- Gender: Required, valid enum value

---

### 2. User Login

**Endpoint:** `POST /login`

**Description:** Authenticate user and receive access tokens

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "userId": 123,
    "email": "user@example.com",
    "fullName": "John Doe",
    "profileImageUrl": null,
    "subscriptionTier": "Free",
    "subscriptionStatus": "Active",
    "subscriptionExpiresAt": null,
    "lastLoginAt": "2024-01-15T10:30:00Z",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenExpiresAt": "2024-01-15T10:50:00Z"
  },
  "message": "Login successful",
  "errors": null
}
```

**Error Responses:**

**Invalid Credentials (400):**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Login failed",
  "errors": {
    "Credentials": ["Invalid email or password"]
  }
}
```

**Email Not Confirmed (400):**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Login failed",
  "errors": {
    "Email": ["Please confirm your email address before logging in"]
  }
}
```

**Account Locked (400):**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Login failed",
  "errors": {
    "Account": ["Account is locked until 2024-01-15 12:00:00 UTC"]
  }
}
```

**Account Disabled (400):**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Login failed",
  "errors": {
    "Account": ["Account has been disabled. Please contact support."]
  }
}
```

**Security Features:**
- ✅ Timing attack protection
- ✅ Email confirmation requirement
- ✅ Account lockout detection
- ✅ Account status validation
- ✅ Role assignment verification

---

### 3. Debug Login (Development Only)

**Endpoint:** `POST /debug-login`

**Description:** Authenticate user bypassing email confirmation (for testing only)

⚠️ **Warning:** Remove this endpoint in production

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:** Same as regular login endpoint

---

### 4. Refresh Token

**Endpoint:** `POST /refresh-token`

**Description:** Obtain new access token using refresh token

**Request Body:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": 123
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "userId": 123,
    "email": "user@example.com",
    "fullName": "John Doe",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenExpiresAt": "2024-01-15T11:10:00Z"
  },
  "message": "Token refreshed successfully",
  "errors": null
}
```

**Error Response (400):**
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Token refresh failed",
  "errors": {
    "Token": ["Invalid or expired refresh token"]
  }
}
```

---

### 5. User Logout

**Endpoint:** `POST /logout`

**Description:** Invalidate user tokens and logout

**Request Body:**
```json
{
  "userId": 123,
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "message": "Logged out successfully"
  },
  "message": "Logout successful",
  "errors": null
}
```

---

### 6. Email Confirmation

**Endpoint:** `POST /confirm-email`

**Description:** Confirm user email address with verification token

**Request Body:**
```json
{
  "userId": 123,
  "token": "base64-encoded-token"
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "message": "Email confirmed successfully"
  },
  "message": "Email confirmation successful",
  "errors": null
}
```

---

### 7. Send Password Reset

**Endpoint:** `POST /send-password-reset`

**Description:** Send password reset email to user

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "message": "Password reset email sent successfully"
  },
  "message": "Reset email sent",
  "errors": null
}
```

---

### 8. Reset Password

**Endpoint:** `POST /reset-password`

**Description:** Reset user password using reset token

**Request Body:**
```json
{
  "email": "user@example.com",
  "token": "reset-token",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "message": "Password reset successfully"
  },
  "message": "Password reset successful",
  "errors": null
}
```

---

## Protected Endpoints

### 9. Get User Profile

**Endpoint:** `GET /profile`

**Authentication:** Required (Bearer Token)

**Description:** Get current authenticated user's profile

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "userId": "123",
  "email": "user@example.com",
  "role": "Customer",
  "message": "Authentication working correctly!"
}
```

**Error Response (401):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

---

### 10. Admin Only Endpoint

**Endpoint:** `GET /admin-only`

**Authentication:** Required (Bearer Token + Admin Role)

**Description:** Test endpoint for admin role verification

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Success Response (200):**
```json
{
  "message": "Admin access granted!",
  "userId": "123",
  "email": "admin@example.com",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

**Error Response (403):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403
}
```

---

### 11. Revoke User (Admin Only)

**Endpoint:** `POST /revoke-user`

**Authentication:** Required (Bearer Token + Admin Role)

**Description:** Revoke user account access (admin only)

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Request Body:**
```json
{
  "userId": 456,
  "reason": "Policy violation"
}
```

**Success Response (200):**
```json
{
  "isSucceeded": true,
  "data": {
    "message": "User access revoked successfully"
  },
  "message": "User revoked",
  "errors": null
}
```

---

## Token Information

### JWT Access Token
- **Expiration:** 20 minutes
- **Claims:**
  - `sub`: User ID
  - `email`: User email
  - `role`: User role
- **Algorithm:** HMAC SHA256

### JWT Refresh Token
- **Expiration:** 30 days (43,200 minutes)
- **Claims:**
  - `sub`: User ID
- **Algorithm:** HMAC SHA256

### Token Usage
Include the access token in the `Authorization` header:
```
Authorization: Bearer <access_token>
```

---

## Error Codes

| Status Code | Description |
|-------------|-------------|
| 200 | Success |
| 400 | Bad Request - Validation errors or business logic errors |
| 401 | Unauthorized - Invalid or missing token |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 500 | Internal Server Error - Server error |

---

## Security Features

### Implemented Security Measures
- ✅ **JWT Token Authentication** with secure signing
- ✅ **Refresh Token Rotation** for enhanced security
- ✅ **Email Confirmation** requirement
- ✅ **Account Lockout** protection
- ✅ **Timing Attack Protection** in login
- ✅ **Role-Based Authorization** (Customer, Admin)
- ✅ **Password Validation** with ASP.NET Core Identity
- ✅ **Token Expiration** management
- ✅ **Account Status** validation

### Best Practices
- Use HTTPS in production
- Store tokens securely on client side
- Implement proper token refresh logic
- Handle token expiration gracefully
- Validate all user inputs
- Log security events appropriately

---

## Example Usage Flow

1. **Register** new user account
2. **Confirm email** using verification link
3. **Login** to receive access and refresh tokens
4. **Access protected endpoints** using Bearer token
5. **Refresh tokens** before expiration
6. **Logout** to invalidate tokens

---

**Note:** This API follows REST conventions and returns consistent response formats for all endpoints. 