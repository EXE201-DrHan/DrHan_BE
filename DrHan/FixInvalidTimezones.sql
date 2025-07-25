-- Fix Invalid Timezone Settings
-- This script sets ALL users to Vietnam timezone for consistent notification processing

-- Update ALL users to Vietnam timezone
UPDATE UserMealNotificationSettings 
SET TimeZone = 'SE Asia Standard Time'  -- Vietnam timezone for Windows
WHERE TimeZone != 'SE Asia Standard Time';

-- Verify the fix
SELECT 
    UserId,
    TimeZone,
    IsEnabled,
    CreateAt,
    UpdateAt
FROM UserMealNotificationSettings 
WHERE TimeZone = 'SE Asia Standard Time'
ORDER BY UpdateAt DESC;

-- Show any remaining invalid timezones (if any)
SELECT 
    UserId,
    TimeZone,
    IsEnabled
FROM UserMealNotificationSettings 
WHERE TimeZone NOT IN (
    SELECT Id FROM sys.time_zone_info
); 