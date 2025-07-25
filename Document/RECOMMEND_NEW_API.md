# RecommendNew API Documentation

## Overview

The RecommendNew service provides **intelligent recipe recommendations** based on:
- 🕐 **Current time** (automatically determines meal type)
- 📊 **User's past meal plans** (analyzes preferences from last 3 months)
- 🚫 **Allergen exclusions** (automatically filters out user's allergens)
- 🍽️ **Meal type optimization** (breakfast, lunch, dinner, snack)

## Core Features

✅ **Time-Based Recommendations** - Automatically suggests appropriate meals based on current time  
✅ **Personalized Preferences** - Learns from user's past meal plan choices  
✅ **Allergy-Safe** - Automatically excludes recipes with user's allergens  
✅ **Smart Scoring** - Multi-factor algorithm considering rating, preferences, and variety  
✅ **Intelligent Caching** - High-performance recommendations with cache optimization  

## API Endpoints

### 1. Get Current Time-Based Recommendations

**🍽️ Get personalized recommendations based on current time and your past preferences**

```http
GET /api/recommendnew/recommendations?count=10
Authorization: Bearer YOUR_JWT_TOKEN
```

**Query Parameters:**
- `count` (optional) - Number of recommendations (1-50, default: 10)

**Response:**
```json
{
  "isSucceeded": true,
  "data": [123, 456, 789, 101, 102, 103, 104, 105, 106, 107],
  "message": "Found 10 recommendations for lunch"
}
```

**How it works:**
1. **Time Analysis**: Determines current meal type based on time:
   - 5 AM - 10:59 AM → Breakfast
   - 11 AM - 3:59 PM → Lunch  
   - 4 PM - 8:59 PM → Dinner
   - 9 PM - 4:59 AM → Snack

2. **Preference Analysis**: Analyzes your meal plan history from last 3 months:
   - Cuisine preferences (Italian, Asian, etc.)
   - Meal type preferences (breakfast vs dinner patterns)
   - Recently used recipes (avoids repetition)
   - Favorite recipes (high completion rate)

3. **Allergy Filtering**: Automatically excludes recipes containing your allergens

4. **Smart Scoring**: Ranks recipes using:
   - Recipe rating (30% weight)
   - Cuisine preference match (25% weight)
   - Meal type preference match (20% weight)
   - Variety (15% weight) - avoids recently used
   - Favorite bonus (10% weight)

---

### 2. Get Specific Meal Type Recommendations

**🍽️ Get recommendations for a specific meal type based on your preferences**

```http
GET /api/recommendnew/recommendations/{mealType}?count=15
Authorization: Bearer YOUR_JWT_TOKEN
```

**Path Parameters:**
- `mealType` - One of: `breakfast`, `lunch`, `dinner`, `snack`

**Query Parameters:**
- `count` (optional) - Number of recommendations (1-50, default: 10)

**Examples:**

**Breakfast Recommendations:**
```http
GET /api/recommendnew/recommendations/breakfast?count=5
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [201, 202, 203, 204, 205],
  "message": "Found 5 breakfast recommendations"
}
```

**Dinner Recommendations:**
```http
GET /api/recommendnew/recommendations/dinner?count=20
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": [301, 302, 303, 304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 320],
  "message": "Found 20 dinner recommendations"
}
```

---

### 3. Get Current Meal Type

**🕐 Get the current meal type suggestion based on time of day**

```http
GET /api/recommendnew/current-meal-type
```

**Response:**
```json
{
  "isSucceeded": true,
  "data": "lunch",
  "message": "Current meal type: lunch"
}
```

**Time Ranges:**
- **Breakfast**: 5:00 AM - 10:59 AM
- **Lunch**: 11:00 AM - 3:59 PM  
- **Dinner**: 4:00 PM - 8:59 PM
- **Snack**: 9:00 PM - 4:59 AM

---

## Error Responses

### Invalid Count Parameter
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Count must be between 1 and 50"
}
```

### Invalid Meal Type
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "Meal type must be one of: breakfast, lunch, dinner, snack"
}
```

### No Recommendations Found
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "No suitable recommendations found based on your preferences"
}
```

### Authentication Required
```json
{
  "isSucceeded": false,
  "data": null,
  "message": "User not authenticated"
}
```

---

## Algorithm Details

### 1. Time-Based Meal Type Detection

```csharp
private string GetMealTypeByCurrentTime()
{
    var currentHour = DateTime.Now.Hour;
    
    return currentHour switch
    {
        >= 5 and < 11 => "breakfast",  // 5 AM - 10:59 AM
        >= 11 and < 16 => "lunch",     // 11 AM - 3:59 PM
        >= 16 and < 21 => "dinner",    // 4 PM - 8:59 PM
        _ => "snack"                   // 9 PM - 4:59 AM
    };
}
```

### 2. User Preference Analysis

The system analyzes your meal plan history from the **last 3 months**:

#### **Cuisine Preferences**
- Tracks which cuisines you choose most often
- Calculates preference ratios (e.g., 40% Italian, 30% Asian, 30% Others)
- Considers completion rates (did you actually cook and complete the meal?)

#### **Meal Type Patterns**
- Analyzes your breakfast vs dinner preferences
- Identifies patterns in meal complexity choices
- Tracks timing preferences

#### **Recipe Usage Patterns**
- **Recently Used**: Recipes from last 2 weeks (avoided for variety)
- **Favorites**: Recipes with high completion rates (≥80% completion)
- **Variety Score**: Encourages trying new recipes while respecting preferences

### 3. Smart Scoring Algorithm

```csharp
private double CalculateRecommendationScore(Recipe recipe, UserCuisinePreferences preferences, string mealType)
{
    var score = 0.0;

    // Recipe rating (30% weight)
    var rating = (double)(recipe.RatingAverage ?? 0);
    score += rating * 0.3;

    // Cuisine preference match (25% weight)
    var cuisinePreference = preferences.CuisinePreferences
        .FirstOrDefault(cp => cp.CuisineType == recipe.CuisineType);
    if (cuisinePreference != null)
    {
        score += cuisinePreference.PreferenceRatio * 0.25;
    }

    // Meal type preference match (20% weight)
    var mealTypePreference = preferences.MealTypePreferences
        .FirstOrDefault(mp => mp.MealType == recipe.MealType);
    if (mealTypePreference != null)
    {
        score += mealTypePreference.PreferenceRatio * 0.20;
    }

    // Variety score (15% weight) - avoid recently used
    if (!preferences.RecentlyUsedRecipeIds.Contains(recipe.Id))
    {
        score += 0.15;
    }

    // Favorite recipe bonus (10% weight)
    if (preferences.FavoriteRecipeIds.Contains(recipe.Id))
    {
        score += 0.10;
    }

    return Math.Max(0.0, Math.Min(5.0, score));
}
```

### 4. Caching Strategy

- **User Allergies**: Cached for 2 hours
- **User Preferences**: Cached for 1 hour  
- **Recommendations**: Cached for 15 minutes
- **Fallback**: Returns top-rated recipes if no preferences found

---

## Usage Examples

### Frontend Integration

```javascript
// Get current time-based recommendations
async function getCurrentRecommendations() {
    const response = await fetch('/api/recommendnew/recommendations?count=10', {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    const result = await response.json();
    return result.data; // Array of recipe IDs
}

// Get dinner recommendations
async function getDinnerRecommendations() {
    const response = await fetch('/api/recommendnew/recommendations/dinner?count=15', {
        headers: {
            'Authorization': `Bearer ${token}`
        }
    });
    const result = await response.json();
    return result.data;
}

// Get current meal type
async function getCurrentMealType() {
    const response = await fetch('/api/recommendnew/current-meal-type');
    const result = await response.json();
    return result.data; // "breakfast", "lunch", "dinner", or "snack"
}
```

### Mobile App Integration

```dart
// Flutter example
class RecommendNewService {
  Future<List<int>> getRecommendations({int count = 10}) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/recommendnew/recommendations?count=$count'),
      headers: {'Authorization': 'Bearer $token'},
    );
    
    final data = jsonDecode(response.body);
    return List<int>.from(data['data']);
  }
  
  Future<List<int>> getMealTypeRecommendations(String mealType, {int count = 10}) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/recommendnew/recommendations/$mealType?count=$count'),
      headers: {'Authorization': 'Bearer $token'},
    );
    
    final data = jsonDecode(response.body);
    return List<int>.from(data['data']);
  }
}
```

---

## Performance Considerations

### **Caching Benefits**
- User allergies cached for 2 hours (rarely change)
- User preferences cached for 1 hour (analyzed from 3-month history)
- Recommendations cached for 15 minutes (balances freshness vs performance)

### **Database Optimization**
- Uses indexed fields for meal type and cuisine filtering
- Includes only necessary related data (allergens, nutrition)
- Limits result sets to prevent memory issues

### **Fallback Strategy**
- Returns top-rated recipes if no user preferences found
- Graceful degradation if preference analysis fails
- Error handling with meaningful messages

---

## Best Practices

### **For Frontend Developers**
1. **Cache Recommendations**: Store results locally for 15 minutes
2. **Progressive Loading**: Start with 5-10 recommendations, load more on demand
3. **Error Handling**: Show fallback content if recommendations fail
4. **User Feedback**: Allow users to rate/complete recommended recipes

### **For Backend Integration**
1. **Rate Limiting**: Consider implementing rate limits for heavy usage
2. **Monitoring**: Track recommendation success rates and user engagement
3. **A/B Testing**: Test different scoring weights for optimization
4. **Data Privacy**: Ensure user preference data is properly secured

---

## Future Enhancements

### **Planned Features**
- 🎯 **Seasonal Recommendations** - Consider seasonal ingredients and preferences
- 🌡️ **Weather Integration** - Suggest warm/cold weather appropriate meals
- 🏃 **Activity Level** - Consider user's activity level for calorie needs
- 👥 **Family Preferences** - Include family member preferences for family plans
- 📱 **Push Notifications** - Suggest meals at appropriate times

### **Advanced Analytics**
- 📊 **Recommendation Success Tracking** - Monitor which recommendations lead to meal completions
- 🧠 **Machine Learning Integration** - Improve scoring based on user behavior patterns
- 🎨 **Personalization Engine** - More sophisticated preference learning algorithms 