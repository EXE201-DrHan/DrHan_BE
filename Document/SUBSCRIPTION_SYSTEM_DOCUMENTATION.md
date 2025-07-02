# DrHan Subscription System Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Subscription Flow](#subscription-flow)
4. [Components](#components)
5. [API Endpoints](#api-endpoints)
6. [Data Models](#data-models)
7. [Background Services](#background-services)
8. [Usage Examples](#usage-examples)
9. [Integration Guide](#integration-guide)

---

## Overview

The DrHan Subscription System is a comprehensive subscription management platform that handles:
- **Subscription Lifecycle Management** (Create, Cancel, Renew, Upgrade)
- **Payment Processing Integration** with PayOS
- **Feature Access Control** and Usage Tracking
- **History Management** (Purchase, Usage, Subscription)
- **Automatic Subscription Expiry Handling**

### Key Features
✅ **CQRS Pattern Implementation**  
✅ **Automatic Payment-to-Subscription Activation**  
✅ **Real-time Feature Access Control**  
✅ **Usage Tracking and Limits**  
✅ **Background Expiry Processing**  
✅ **Comprehensive History Views**  
✅ **Flexible Billing Cycles**  

---

## Architecture

### System Architecture Overview
```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Controllers   │    │   Application   │    │ Infrastructure  │
│                 │    │                 │    │                 │
│ SubscriptionCtrl│◄──►│ CQRS Commands  │◄──►│ SubscriptionSvc │
│ PaymentCtrl     │    │ CQRS Queries   │    │ PayOSService    │
│ UserAllergyCtrl │    │ DTOs/Mappers   │    │ Background Svc  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   API Layer     │    │ Business Logic  │    │  Data Access    │
│ - Authentication│    │ - Validation    │    │ - Entity Framework│
│ - Authorization │    │ - Business Rules│    │ - Repository    │
│ - Error Handling│    │ - AutoMapper    │    │ - UnitOfWork    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

### CQRS Implementation
The system follows **Command Query Responsibility Segregation (CQRS)**:

**Commands** (Write Operations):
- `CreateSubscriptionCommand`
- `CancelSubscriptionCommand`
- `RenewSubscriptionCommand`
- `UpgradeSubscriptionCommand`

**Queries** (Read Operations):
- `GetSubscriptionStatusQuery`
- `GetPurchaseHistoryQuery`
- `GetUsageHistoryQuery`
- `GetSubscriptionHistoryQuery`

---

## Subscription Flow

### Complete Subscription Lifecycle

```
User Requests Subscription
          ↓
Create Subscription Command
          ↓
Subscription Created - PENDING Status
          ↓
Generate Payment Link
          ↓
User Makes Payment via PayOS
          ↓
Payment Successful? ──No──► Payment Failed ──► Mark Subscription INACTIVE
          ↓ Yes
PayOS Webhook Received
          ↓
ActivateSubscriptionAsync
          ↓
Update Status to ACTIVE
          ↓
Set Start/End Dates
          ↓
Refresh Plan Cache
          ↓
Subscription Active
          ↓
User Can Access Features
          ↓
Usage Tracking
          ↓
Subscription Expired? ──No──► Continue Usage
          ↓ Yes
Background Service
          ↓
Mark as EXPIRED
          ↓
Block Feature Access
```

### Detailed Flow Steps

#### 1. **Subscription Creation**
```json
POST /api/subscription
{
  "planId": 2
}
```
- Validates user doesn't have active subscription
- Creates subscription with `PENDING` status
- Returns subscription details for payment processing

#### 2. **Payment Processing**
```json
POST /api/payment
{
  "amount": 99.99,
  "currency": "USD", 
  "userSubscriptionId": 123
}
```
- PayOS generates payment link
- User completes payment
- PayOS sends webhook on completion

#### 3. **Automatic Activation**
```csharp
// In PayOSService.HandleWebhookAsync()
if (webhook.Code == "00" && webhook.Desc == "success")
{
    await ActivateSubscriptionAsync(payment);
}
```
- Webhook triggers subscription activation
- Sets `ACTIVE` status and calculates end date
- Refreshes feature cache

#### 4. **Feature Access Control**
```
GET /api/subscription/can-use/recipe_generation
```
- Real-time feature access checking
- Usage limit enforcement
- Plan-based permissions

#### 5. **Usage Tracking**
```json
POST /api/subscription/track-usage
{
  "featureName": "meal_planning",
  "count": 1
}
```
- Automatic usage tracking
- Daily/monthly aggregation
- Resource usage logging

#### 6. **Background Expiry Processing**
```csharp
// SubscriptionExpiryService runs every hour
foreach (var expired in expiredSubscriptions)
{
    expired.Status = UserSubscriptionStatus.Expired;
}
```
- Automatic expiry detection
- Status updates
- Renewal notifications (ready for integration)

---

## Components

### 1. **Core Services**

#### **SubscriptionService**
```csharp
public interface ISubscriptionService
{
    Task<bool> HasActiveSubscription(int userId);
    Task<bool> CanUseFeature(int userId, string featureName, string limitType = "daily");
    Task<SubscriptionPlan> GetUserPlan(int userId);
    Task TrackUsage(int userId, string featureName, int count = 1);
    Task<int> GetUsageCount(int userId, string featureName, DateTime? fromDate = null);
    Task<Dictionary<string, object>> GetPlanLimits(SubscriptionPlan plan);
    Task RefreshPlanCache();
}
```

**Key Responsibilities:**
- Feature access control
- Usage tracking and limits
- Plan management
- Cache management

#### **PayOSService** (Enhanced)
```csharp
// New integration methods
private async Task ActivateSubscriptionAsync(Payment payment);
private async Task HandleFailedPaymentAsync(Payment payment);
```

**Enhanced Features:**
- Automatic subscription activation on successful payment
- Failed payment handling
- Subscription lifecycle integration

### 2. **CQRS Commands**

#### **CreateSubscriptionCommand**
```csharp
public class CreateSubscriptionCommand : IRequest<AppResponse<SubscriptionResponseDto>>
{
    public int UserId { get; set; }
    public int PlanId { get; set; }
}
```
- Validates plan exists and is active
- Checks for existing active subscriptions
- Creates subscription with PENDING status

#### **CancelSubscriptionCommand**
```csharp
public class CancelSubscriptionCommand : IRequest<AppResponse<bool>>
{
    public int UserId { get; set; }
    public string? CancellationReason { get; set; }
}
```
- Immediate cancellation
- Optional cancellation reason tracking
- Status change to CANCELLED

#### **UpgradeSubscriptionCommand**
```csharp
public class UpgradeSubscriptionCommand : IRequest<AppResponse<SubscriptionResponseDto>>
{
    public int UserId { get; set; }
    public int NewPlanId { get; set; }
}
```
- Validates upgrade path (higher tier only)
- Updates plan association
- Maintains current billing cycle

### 3. **Background Services**

#### **SubscriptionExpiryService**
```csharp
public class SubscriptionExpiryService : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
}
```

**Functionality:**
- Runs every hour
- Identifies expired subscriptions
- Updates statuses automatically
- Logs renewal opportunities
- Ready for notification integration

---

## API Endpoints

### Subscription Management

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/subscription/status` | Get current subscription status | ✅ |
| `POST` | `/api/subscription` | Create new subscription | ✅ |
| `POST` | `/api/subscription/cancel` | Cancel subscription | ✅ |
| `POST` | `/api/subscription/renew` | Renew subscription | ✅ |
| `POST` | `/api/subscription/upgrade` | Upgrade to higher plan | ✅ |

### Feature Access Control

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/subscription/can-use/{feature}` | Check feature access | ✅ |
| `POST` | `/api/subscription/track-usage` | Track feature usage | ✅ |
| `GET` | `/api/subscription/usage/{feature}` | Get usage statistics | ✅ |

### History Management

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/subscription/history/purchases` | Get purchase history | ✅ |
| `GET` | `/api/subscription/history/usage` | Get usage history | ✅ |
| `GET` | `/api/subscription/history/subscriptions` | Get subscription history | ✅ |

### User Allergy Integration

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/userallergy/has-allergies` | Check if user has allergies | ✅ |
| `GET` | `/api/userallergy` | Get user's allergies | ✅ |
| `GET` | `/api/userallergy/profile` | Get allergy profile | ✅ |

---

## Data Models

### Core Entities

#### **UserSubscription**
```csharp
public class UserSubscription : BaseEntity
{
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public UserSubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation Properties
    public virtual SubscriptionPlan Plan { get; set; }
    public virtual ApplicationUser ApplicationUser { get; set; }
}
```

#### **SubscriptionPlan**
```csharp
public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }
    public string BillingCycle { get; set; }
    public int? UsageQuota { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation Properties
    public virtual ICollection<PlanFeature> PlanFeatures { get; set; }
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; }
}
```

#### **Payment**
```csharp
public class Payment : BaseEntity
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string TransactionId { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? FailureReason { get; set; }
    public int UserSubscriptionId { get; set; }
    
    // Navigation Properties  
    public virtual UserSubscription? UserSubscription { get; set; }
}
```

#### **SubscriptionUsage**
```csharp
public class SubscriptionUsage : BaseEntity
{
    public int UserSubscriptionId { get; set; }
    public string FeatureType { get; set; }
    public int UsageCount { get; set; }
    public DateTime UsageDate { get; set; }
    public string ResourceUsed { get; set; }
    
    // Navigation Properties
    public virtual UserSubscription UserSubscription { get; set; }
}
```

### Status Enums

#### **UserSubscriptionStatus**
```csharp
public enum UserSubscriptionStatus
{
    Active,    // Subscription is active and usable
    Inactive,  // Subscription is inactive
    Cancelled, // User cancelled subscription
    Expired,   // Subscription has expired
    Pending    // Waiting for payment confirmation
}
```

#### **PaymentStatus**
```csharp
public enum PaymentStatus
{
    Pending,  // Payment is being processed
    Success,  // Payment completed successfully
    Failed    // Payment failed
}
```

### DTOs

#### **SubscriptionResponseDto**
```csharp
public class SubscriptionResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PlanName { get; set; }
    public decimal PlanPrice { get; set; }
    public string BillingCycle { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public UserSubscriptionStatus Status { get; set; }
    public bool IsActive { get; set; }
    public int? DaysRemaining { get; set; }
}
```

#### **SubscriptionStatusDto**
```csharp
public class SubscriptionStatusDto
{
    public bool HasActiveSubscription { get; set; }
    public SubscriptionResponseDto? CurrentSubscription { get; set; }
    public Dictionary<string, object>? PlanLimits { get; set; }
    public Dictionary<string, int>? CurrentUsage { get; set; }
}
```

---

## Usage Examples

### 1. **Creating a Subscription**

```bash
# Step 1: Create subscription
POST /api/subscription
{
  "planId": 2
}

# Response
{
  "isSucceeded": true,
  "data": {
    "id": 123,
    "userId": 456,
    "planName": "Premium Plan",
    "planPrice": 99.99,
    "status": "Pending",
    "isActive": false
  }
}

# Step 2: Process payment
POST /api/payment
{
  "amount": 99.99,
  "currency": "USD",
  "userSubscriptionId": 123
}

# Step 3: Payment webhook activates subscription automatically
# No additional API calls needed
```

### 2. **Checking Feature Access**

```bash
# Check if user can generate recipes
GET /api/subscription/can-use/recipe_generation?limitType=daily

# Response
{
  "canUse": true,
  "featureName": "recipe_generation",
  "limitType": "daily"
}

# Track usage after feature use
POST /api/subscription/track-usage
{
  "featureName": "recipe_generation",
  "count": 1
}
```

### 3. **Viewing History**

```bash
# Get purchase history
GET /api/subscription/history/purchases?pageNumber=1&pageSize=10

# Get usage history for specific feature
GET /api/subscription/history/usage?featureType=meal_planning&pageNumber=1

# Get subscription timeline
GET /api/subscription/history/subscriptions
```

### 4. **Subscription Management**

```bash
# Check current status
GET /api/subscription/status

# Cancel subscription
POST /api/subscription/cancel
{
  "cancellationReason": "Too expensive"
}

# Upgrade plan
POST /api/subscription/upgrade
{
  "newPlanId": 3
}
```

---

## Integration Guide

### 1. **PayOS Integration**

#### **Webhook Configuration**
```csharp
[HttpPost("webhook")]
public async Task<ActionResult> HandleWebhook([FromBody] PayOSWebhookDto webhook)
{
    var success = await _payOSService.HandleWebhookAsync(webhook);
    return success ? Ok() : BadRequest();
}
```

#### **Automatic Activation Flow**
```csharp
// PayOSService automatically handles:
if (webhook.Code == "00" && webhook.Desc == "success")
{
    // 1. Update payment status
    payment.PaymentStatus = PaymentStatus.Success;
    
    // 2. Activate subscription
    await ActivateSubscriptionAsync(payment);
    
    // 3. Set billing dates
    subscription.StartDate = DateTime.UtcNow;
    subscription.EndDate = CalculateEndDate(plan.BillingCycle);
    
    // 4. Refresh cache
    await _subscriptionService.RefreshPlanCache();
}
```

### 2. **Feature Integration**

#### **Before Feature Use**
```csharp
public async Task<IActionResult> GenerateRecipe()
{
    var userId = _userContext.GetCurrentUserId();
    var canUse = await _subscriptionService.CanUseFeature(userId, "recipe_generation");
    
    if (!canUse)
    {
        return Forbid("Feature usage limit exceeded or subscription required");
    }
    
    // Proceed with feature
    var recipe = await _recipeService.GenerateRecipe();
    
    // Track usage
    await _subscriptionService.TrackUsage(userId, "recipe_generation");
    
    return Ok(recipe);
}
```

### 3. **Plan Configuration**

#### **Database Setup**
```sql
-- Example plans
INSERT INTO SubscriptionPlans (Name, Description, Price, Currency, BillingCycle, UsageQuota, IsActive)
VALUES 
('Free', 'Basic features', 0, 'USD', 'monthly', 10, 1),
('Premium', 'Enhanced features', 9.99, 'USD', 'monthly', 100, 1),
('Pro', 'Unlimited access', 19.99, 'USD', 'monthly', NULL, 1);

-- Plan features
INSERT INTO PlanFeatures (PlanId, FeatureName, Description, IsEnabled)
VALUES 
(1, 'recipe_generation', 'Generate recipes', 1),
(1, 'meal_planning', 'Create meal plans', 1),
(2, 'recipe_generation', 'Generate recipes', 1),
(2, 'meal_planning', 'Create meal plans', 1),
(2, 'smart_recommendations', 'AI recommendations', 1);
```

### 4. **Caching Strategy**

#### **Plan Features Cache**
```csharp
// Automatic cache management
private async Task<Dictionary<string, PlanFeature>> GetPlanFeaturesFromCache(SubscriptionPlan plan)
{
    var cacheKey = $"subscription_plan_features_{plan.Id}";
    
    if (_cache.TryGetValue(cacheKey, out var cachedFeatures))
        return cachedFeatures;
    
    var features = await LoadPlanFeatures(plan.Id);
    _cache.Set(cacheKey, features, TimeSpan.FromMinutes(30));
    
    return features;
}
```

### 5. **Error Handling**

#### **Common Error Scenarios**
```csharp
// Subscription not found
{
  "isSucceeded": false,
  "message": "No active subscription found for user"
}

// Feature limit exceeded
{
  "canUse": false,
  "message": "Daily usage limit exceeded"
}

// Payment failed
{
  "isSucceeded": false,
  "message": "Payment processing failed"
}
```

---

## Best Practices

### 1. **Security**
- All endpoints require authentication
- User can only access their own data
- Admin endpoints properly protected
- Payment webhooks validated

### 2. **Performance**
- Pagination for all history endpoints
- Efficient database queries with includes
- Caching for plan features
- Background processing for heavy operations

### 3. **Reliability**
- Comprehensive error handling
- Transaction management
- Retry logic for critical operations
- Extensive logging

### 4. **Scalability**
- CQRS pattern for separation of concerns
- Background services for async processing
- Repository pattern for data access
- Service layer abstraction

---

## Monitoring and Logging

### Key Metrics to Monitor
- Subscription creation rate
- Payment success rate
- Feature usage patterns
- Subscription churn rate
- System performance

### Log Events
```csharp
// Subscription events
_logger.LogInformation("Subscription {SubscriptionId} activated for user {UserId}");
_logger.LogWarning("Payment {PaymentId} failed for user {UserId}");

// Feature usage
_logger.LogInformation("User {UserId} used feature {FeatureName}");
_logger.LogWarning("User {UserId} exceeded limit for {FeatureName}");

// Background processing
_logger.LogInformation("Processed {Count} expired subscriptions");
```

---

## Conclusion

The DrHan Subscription System provides a comprehensive, scalable solution for subscription management with:

✅ **Complete Lifecycle Management** - From creation to expiry  
✅ **Seamless Payment Integration** - Automatic activation flow  
✅ **Real-time Feature Control** - Usage tracking and limits  
✅ **Comprehensive History** - Full transparency for users  
✅ **Background Processing** - Automatic maintenance  
✅ **Production Ready** - Error handling, logging, security  

The system is designed to be maintainable, scalable, and user-friendly, providing both developers and end-users with the tools they need for effective subscription management. 