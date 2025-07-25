# Meal Notification Timezone Configuration

## Overview

The meal notification system is configured to **always use Vietnam timezone** for processing and sending notifications, regardless of user location or timezone settings.

## Why Vietnam Timezone?

- **Consistency**: All notifications are processed at the same time for all users
- **Simplicity**: No complex timezone conversion logic needed
- **Reliability**: Eliminates timezone-related errors and edge cases
- **Business Logic**: Aligns with the application's target market (Vietnam)

## How It Works

### 1. Notification Processing
```csharp
// Always use Vietnam time for notification processing
var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(now, VietnamTimeZone);
var vietnamDate = DateOnly.FromDateTime(vietnamTime);
```

### 2. User Settings
- All users have their timezone automatically set to `'SE Asia Standard Time'` (Vietnam)
- User timezone preferences are ignored for notification processing
- Timezone field is kept for potential future use but not used in current logic

### 3. Meal Times
- Breakfast: 8:00 AM Vietnam time
- Lunch: 12:00 PM Vietnam time  
- Dinner: 6:30 PM Vietnam time
- Snack: 3:00 PM Vietnam time

### 4. Quiet Hours
- Start: 10:00 PM Vietnam time
- End: 7:00 AM Vietnam time

## API Endpoints

### Get Available Timezones
```http
GET /api/mealnotifications/timezones
```
Returns all available timezone IDs (for reference only, not used in processing).

### Update Settings
```http
PUT /api/mealnotifications/settings
```
The `timeZone` field in the request is ignored - all users get Vietnam timezone.

## Database

### UserMealNotificationSettings Table
```sql
-- All users have this timezone
TimeZone = 'SE Asia Standard Time'
```

### Fix Script
Run `FixInvalidTimezones.sql` to ensure all users have Vietnam timezone:
```sql
UPDATE UserMealNotificationSettings 
SET TimeZone = 'SE Asia Standard Time'
WHERE TimeZone != 'SE Asia Standard Time';
```

## Benefits

✅ **No Timezone Errors**: Eliminates `TimeZoneNotFoundException` errors  
✅ **Consistent Behavior**: All users get notifications at the same Vietnam time  
✅ **Simplified Logic**: No complex timezone conversion calculations  
✅ **Reliable Scheduling**: Notifications are processed based on a single timezone  
✅ **Easy Debugging**: All timestamps are in Vietnam time  

## Future Considerations

If multi-timezone support is needed in the future:
1. Add timezone conversion logic back
2. Use user's preferred timezone for processing
3. Handle daylight saving time transitions
4. Add timezone validation and fallback logic

## Current Implementation

- **Processing Time**: Vietnam timezone (UTC+7)
- **Storage**: All users stored with Vietnam timezone
- **API**: Timezone field ignored, always uses Vietnam
- **Logging**: All timestamps logged in Vietnam time 