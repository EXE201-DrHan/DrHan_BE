# 🔐 Authentication API Documentation

## Overview
The Authentication API provides comprehensive user authentication and account management functionality including registration, login, email verification, password management, and account recovery features.

**Base URL**: `/api/authentication`  
**Content-Type**: `application/json`

---

## 🚀 Authentication Endpoints

### Register User
**POST** `/register`

Register a new user account with email verification.

### Login User  
**POST** `/login`

Authenticate user with email and password.

### Logout User
**POST** `/logout`

Logout user and revoke active tokens.

### Refresh Token
**POST** `/refresh-token`

Refresh expired access token using refresh token.

### Verify OTP
**POST** `/verify-otp`

Verify OTP code for email verification or other purposes.

---

## 🔄 OTP & Account Recovery Endpoints

### Resend OTP
**POST** `/resend-otp`

Resend OTP code when the original code expires or is lost. Works with email address only for simplicity and consistency.

#### Request Body
```json
{
  "email": "user@example.com"
}
```

#### Parameters
- `email` (string, required) - User email address

#### Response (200 OK)
```json
{
  "isSucceeded": true,
  "data": {
    "isSuccess": true,
    "message": "OTP has been sent to your email address",
    "expiresAt": "2024-01-01T10:05:00Z",
    "remainingAttempts": 3
  },
  "messages": {
    "Success": "OTP resent successfully"
  }
}
```

#### Error Responses
- **400 Bad Request** - Invalid request parameters
- **404 Not Found** - User not found
- **429 Too Many Requests** - Rate limited (must wait between requests)

**Rate Limiting**: 1 minute minimum between resend requests per user.

---

### Reactivate Account
**POST** `/reactivate-account`

Reactivate abandoned user accounts that never completed email verification. This endpoint helps users who registered but never verified their email to complete the process.

#### Request Body
```json
{
  "email": "abandoned@example.com"
}
```

#### Parameters
- `email` (string, required) - Email address of the abandoned account

#### Response (200 OK - Account Found)
```json
{
  "isSucceeded": true,
  "data": {
    "isSuccess": true,
    "message": "Account reactivation OTP has been sent to your email address",
    "userId": 123,
    "accountExists": true,
    "isAlreadyVerified": false,
    "otpExpiresAt": "2024-01-01T10:05:00Z",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123def456..."
  },
  "messages": {
    "Success": "Account reactivation OTP sent successfully"
  }
}
```

#### Response (200 OK - Already Verified)
```json
{
  "isSucceeded": true,
  "data": {
    "isSuccess": false,
    "message": "Account is already verified. You can proceed to login",
    "userId": 123,
    "accountExists": true,
    "isAlreadyVerified": true,
    "otpExpiresAt": "0001-01-01T00:00:00Z",
    "accessToken": "",
    "refreshToken": ""
  },
  "messages": {
    "AlreadyVerified": "Account is already verified"
  }
}
```

#### Response (200 OK - Account Not Found)
```json
{
  "isSucceeded": true,
  "data": {
    "isSuccess": false,
    "message": "If an account with this email exists, an OTP has been sent to your email address",
    "userId": 0,
    "accountExists": false,
    "isAlreadyVerified": false,
    "otpExpiresAt": "0001-01-01T00:00:00Z",
    "accessToken": "",
    "refreshToken": ""
  },
  "messages": {
    "Info": "Account reactivation request processed"
  }
}
```

#### Error Responses
- **400 Bad Request** - Invalid email format
- **429 Too Many Requests** - Rate limited (2 minute minimum between reactivation requests)
- **500 Internal Server Error** - Server error

**Rate Limiting**: 2 minutes minimum between reactivation requests per email.

---

## 🔄 Complete Account Recovery Flow

### Scenario 1: User Has Tokens (Authenticated)
1. User calls `POST /resend-otp` with `userId` from their token
2. New OTP is sent to their registered email
3. User calls `POST /verify-otp` with the new code
4. Account is verified and user can proceed

### Scenario 2: User Lost Tokens (Unauthenticated)
1. User calls `POST /resend-otp` with their `email`
2. New OTP is sent if account exists and is unverified
3. User needs to get new tokens or use reactivate-account

### Scenario 3: Abandoned Registration
1. User calls `POST /reactivate-account` with their `email`
2. System checks if account exists and is unverified
3. New OTP is sent + fresh tokens are provided
4. User calls `POST /verify-otp` with the code and user ID
5. Account is verified and user can login normally

---

## 🛡️ Security Features

### Rate Limiting
- **Resend OTP**: 1 minute cooldown between requests
- **Reactivate Account**: 2 minute cooldown between requests
- Prevents spam and abuse

### Privacy Protection
- Account existence is not revealed for security
- Consistent response times to prevent enumeration attacks
- Generic messages for non-existent accounts

### Token Management
- Reactivate account provides fresh tokens for seamless flow
- Existing tokens remain valid during OTP resend
- Automatic token cleanup on successful verification

---

## 📱 Frontend Integration Examples

### Resend OTP (Email-Based)
```javascript
const resendOtp = async (email) => {
  const response = await fetch('/api/authentication/resend-otp', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ email })
  });
  
  return await response.json();
};
```

### Reactivate Abandoned Account
```javascript
const reactivateAccount = async (email) => {
  const response = await fetch('/api/authentication/reactivate-account', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email })
  });
  
  const result = await response.json();
  
  if (result.data.isSuccess && result.data.accessToken) {
    // Store tokens and redirect to OTP verification
    localStorage.setItem('accessToken', result.data.accessToken);
    localStorage.setItem('refreshToken', result.data.refreshToken);
    localStorage.setItem('userId', result.data.userId);
    // Navigate to OTP verification page
  }
};
```

### Handle Rate Limiting
```javascript
const handleResendWithRetry = async (requestData) => {
  try {
    const response = await fetch('/api/authentication/resend-otp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestData)
    });
    
    const result = await response.json();
    
    if (!result.isSucceeded && result.messages.RateLimit) {
      // Show countdown timer based on rate limit message
      showCountdownTimer(result.data.message);
    }
    
    return result;
  } catch (error) {
    console.error('Resend OTP failed:', error);
  }
};
```

---

These endpoints solve the common problem of abandoned registrations and provide users with multiple recovery paths to complete their account verification process. 