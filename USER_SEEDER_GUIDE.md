# User Seeder Usage Guide

## Overview

Your **UserSeeder** has been fully integrated into the data management system. It creates roles and users including Admin, Staff, Nutritionist, and Customer accounts with realistic data.

## What UserSeeder Creates

### Roles
- **Admin**: Full system access
- **Staff**: Internal staff members  
- **Nutritionist**: Professional nutritionists
- **Customer**: End users with subscription tiers

### Sample Users
- **Admin**: admin@example.com (AdminUser)
- **Staff**: staff1@example.com, staff2@example.com
- **Nutritionists**: nutritionist1@example.com, nutritionist2@example.com  
- **Customers**: customer1@example.com, customer2@example.com, customer3@example.com, customer4@example.com

**Default Password**: `Password123!` for all users

## Usage Methods

### 1. Programmatic Usage (Recommended)

The UserSeeder is now integrated into `DataManagementService`. Use these methods:

```csharp
// Inject the service
private readonly DataManagementService _dataManagementService;

// Seed users only
await _dataManagementService.SeedUsersAndRolesAsync();

// Seed all data (users + food data)
await _dataManagementService.SeedAllDataAsync();

// Reset users only
await _dataManagementService.ResetUsersAndRolesAsync();

// Clean users only (keeps admin)
await _dataManagementService.CleanUsersAndRolesAsync();

// Get user statistics
var userStats = await _dataManagementService.GetUserStatisticsAsync();

// Check if users exist
bool hasUsers = await _dataManagementService.HasUsersAsync();
```

### 2. API Endpoints

Use these HTTP endpoints to manage users:

#### Seeding Endpoints
```http
POST /api/datamanagement/seed/users
POST /api/datamanagement/seed/all
POST /api/datamanagement/seed/food (food only, no users)
```

#### Cleaning Endpoints
```http
DELETE /api/datamanagement/clean/users
DELETE /api/datamanagement/clean/all
DELETE /api/datamanagement/clean/food (food only, no users)
```

#### Reset Endpoints
```http
POST /api/datamanagement/reset/users
POST /api/datamanagement/reset/all
POST /api/datamanagement/reset/food (food only, no users)
```

#### Statistics Endpoints
```http
GET /api/datamanagement/statistics/users
GET /api/datamanagement/statistics
GET /api/datamanagement/health
```

### 3. Automatic Seeding via Configuration

Update your `appsettings.json` or `appsettings.Development.json`:

```json
{
  "DatabaseSettings": {
    "AutoMigrate": true,
    "AutoSeed": true
  },
  "ClearAndReseedData": false
}
```

**Configuration Options:**
- `AutoMigrate: true` - Automatically runs database migrations
- `AutoSeed: true` - Automatically seeds data if database is empty
- `ClearAndReseedData: true` - Forces complete data reset on startup

## Smart Seeding Logic

The system now has intelligent seeding that:

1. **Empty Database**: Seeds all data (users + food)
2. **Has Food Data, No Users**: Seeds only users
3. **Has Users, No Food Data**: Seeds only food data  
4. **Has Both**: Skips seeding and shows statistics

## Example API Usage

### Seed Users Only
```bash
curl -X POST https://localhost:7217/api/datamanagement/seed/users
```

### Get User Statistics
```bash
curl -X GET https://localhost:7217/api/datamanagement/statistics/users
```

**Response:**
```json
{
  "totalUsers": 9,
  "adminUsers": 1,
  "staffUsers": 2,
  "nutritionistUsers": 2,
  "customerUsers": 4,
  "totalRoles": 4
}
```

### Reset All Users
```bash
curl -X POST https://localhost:7217/api/datamanagement/reset/users
```

## Testing Login

After seeding, you can test login with any of these accounts:

```json
{
  "email": "admin@example.com",
  "password": "Password123!"
}
```

```json
{
  "email": "customer1@example.com", 
  "password": "Password123!"
}
```

## Development vs Production

### Development Environment
- **AutoSeed: true** - Automatically ensures data exists
- Users are seeded if missing
- Safe to reset data during development

### Production Environment  
- **AutoSeed: false** - Manual control over data
- Use API endpoints for controlled seeding
- More careful user management

## User Management Features

### User Cleaning
- Deletes all seeded users **except admin**
- Preserves the admin account for system access
- Maintains referential integrity

### User Statistics
- Total user count by role
- Subscription tier breakdown for customers
- Role distribution analytics

## Advanced Usage

### Custom User Seeding
If you need to modify the seeded users, edit `UserSeeder.cs`:

```csharp
var users = new List<(string FullName, string Email, ...)>
{
    // Add your custom users here
    ("Custom User", "custom@example.com", "CustomUser", UserRoles.Customer, ...),
};
```

### Integration with Other Systems
The UserSeeder works seamlessly with:
- JWT authentication system
- Email services for user verification
- Role-based authorization
- Subscription management

## Troubleshooting

### Common Issues

1. **"Failed to seed users"**
   - Check database connection
   - Verify Identity system is properly configured
   - Check for duplicate emails

2. **"Users not appearing"**
   - Verify `AutoSeed: true` in config
   - Check database connection
   - Use `/health` endpoint to diagnose

3. **"Cannot login with seeded users"**
   - Verify password is `Password123!`
   - Check email confirmation settings
   - Ensure user is in correct role

### Health Check
Use the health endpoint to diagnose issues:
```bash
curl -X GET https://localhost:7217/api/datamanagement/health
```

## Best Practices

1. **Development**: Enable AutoSeed for convenience
2. **Production**: Use manual API-based seeding
3. **Testing**: Use ResetUsersAndRolesAsync() for clean tests
4. **Monitoring**: Check user statistics regularly
5. **Security**: Change default passwords in production

This integrated approach gives you full control over user seeding while maintaining the clean architecture of your data management system. 