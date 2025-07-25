# Recipe Search Performance Optimization Guide

## 🚀 Database Index Recommendations

### **Primary Indexes (Critical for Performance)**

```sql
-- Core recipe search indexes
CREATE INDEX IX_Recipes_SearchOptimized ON Recipes 
(CuisineType, MealType, PrepTimeMinutes, RatingAverage, Name);

-- Full-text search index for recipe names and descriptions
CREATE FULLTEXT INDEX FTX_Recipes_Content ON Recipes (Name, Description);

-- Ingredient search optimization
CREATE INDEX IX_RecipeIngredients_Search ON RecipeIngredients 
(IngredientName, RecipeId);

-- Allergen filtering optimization
CREATE INDEX IX_RecipeAllergens_Type ON RecipeAllergens 
(AllergenType, RecipeId);

-- Allergen-free claims optimization
CREATE INDEX IX_RecipeAllergenFreeClaims_Claim ON RecipeAllergenFreeClaims 
(Claim, RecipeId);

-- Time-based filtering
CREATE INDEX IX_Recipes_Time ON Recipes (PrepTimeMinutes, CookTimeMinutes);

-- Rating and popularity
CREATE INDEX IX_Recipes_Quality ON Recipes (RatingAverage DESC, LikesCount DESC);
```

### **Composite Indexes for Common Query Patterns**

```sql
-- Breakfast search optimization
CREATE INDEX IX_Recipes_Breakfast ON Recipes 
(MealType, PrepTimeMinutes, RatingAverage)
WHERE MealType = 'breakfast';

-- Dinner search optimization  
CREATE INDEX IX_Recipes_Dinner ON Recipes 
(MealType, CookTimeMinutes, RatingAverage)
WHERE MealType = 'dinner';

-- Quick meal search
CREATE INDEX IX_Recipes_Quick ON Recipes 
(PrepTimeMinutes, CookTimeMinutes, RatingAverage)
WHERE PrepTimeMinutes <= 30;

-- Cuisine-specific searches
CREATE INDEX IX_Recipes_Cuisine_Rating ON Recipes 
(CuisineType, RatingAverage DESC, Name);
```

## ⚡ Application-Level Optimizations

### **1. Query Optimization Strategies**

#### **Use Minimal Includes for List Views**
```csharp
// Bad - loads unnecessary data
query.Include(r => r.RecipeIngredients)
     .ThenInclude(ri => ri.Ingredient)
     .Include(r => r.RecipeInstructions)
     .Include(r => r.RecipeNutritions);

// Good - only essential data
query.Include(r => r.RecipeIngredients.Take(5))
     .Include(r => r.RecipeAllergens);
```

#### **Filter Order Optimization**
```csharp
// Start with most selective filters first
.Where(r => r.CuisineType == specificCuisine)        // Most selective
.Where(r => r.MealType == specificMealType)          // Very selective  
.Where(r => r.PrepTimeMinutes <= maxTime)            // Moderately selective
.Where(r => r.Name.Contains(searchTerm))              // Least selective
```

### **2. Caching Strategy**

#### **Memory Cache Implementation**
```csharp
// Cache popular searches for 10 minutes
var cacheKey = $"search_{searchTerm}_{cuisineType}_{mealType}_{page}";
var cacheOptions = new MemoryCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    SlidingExpiration = TimeSpan.FromMinutes(5),
    Priority = CacheItemPriority.High
};
```

#### **Redis Cache for Heavy Queries**
```csharp
// Cache expensive filter combinations
var complexFilterCacheKey = $"complex_filter_{filterHash}";
await _distributedCache.SetStringAsync(complexFilterCacheKey, 
    JsonSerializer.Serialize(results), 
    new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    });
```

### **3. Pagination Optimization**

#### **Efficient Offset Alternative**
```csharp
// Bad - OFFSET becomes slow with large page numbers
.Skip((page - 1) * pageSize).Take(pageSize)

// Better - Use cursor-based pagination for deep pagination
.Where(r => r.Id > lastSeenId)
.OrderBy(r => r.Id)
.Take(pageSize)
```

## 🔄 Background Optimization Tasks

### **1. Search Statistics Collection**
```csharp
// Track popular search terms for pre-caching
public async Task TrackSearchAsync(string searchTerm, int resultCount)
{
    // Log to analytics for cache warming
    await _searchAnalytics.LogSearchAsync(searchTerm, resultCount);
}
```

### **2. Pre-warming Popular Searches**
```csharp
// Background service to cache popular searches
public async Task WarmSearchCacheAsync()
{
    var popularTerms = await _searchAnalytics.GetPopularSearchTermsAsync();
    
    foreach (var term in popularTerms)
    {
        // Pre-execute and cache popular searches
        await ExecuteAndCacheSearchAsync(term);
    }
}
```

## 📊 Performance Monitoring

### **1. Query Performance Metrics**
```csharp
public class SearchPerformanceMetrics
{
    public string SearchTerm { get; set; }
    public TimeSpan DatabaseQueryTime { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public int ResultCount { get; set; }
    public bool UsedCache { get; set; }
    public string FilterComplexity { get; set; }
}
```

### **2. Performance Thresholds**
```csharp
// Alert if queries exceed these thresholds
public static class PerformanceThresholds
{
    public static readonly TimeSpan MaxAcceptableQueryTime = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan MaxAcceptableResponseTime = TimeSpan.FromSeconds(2);
    public static readonly int MaxRecommendedPageSize = 50;
}
```

## 🛠️ Implementation Priority

### **Phase 1: Immediate (< 1 day)**
1. ✅ Add basic search result caching (10-minute expiry)
2. ✅ Optimize query includes (remove unnecessary joins)
3. ✅ Reorder filter conditions (most selective first)
4. Add database indexes for common search patterns

### **Phase 2: Short-term (< 1 week)**
1. Implement full-text search for better text matching
2. Add cursor-based pagination for deep pages
3. Create background cache warming service
4. Add performance monitoring and alerting

### **Phase 3: Long-term (< 1 month)**
1. Implement distributed caching (Redis)
2. Add search analytics and optimization
3. Create specialized search indexes
4. Implement search result pre-computation for popular terms

## 🎯 Expected Performance Improvements

### **Before Optimization**
- Average query time: 2-5 seconds
- Database load: High for complex filters
- Cache hit rate: 0%

### **After Phase 1 Optimization**
- Average query time: 200-500ms
- Database load: Reduced by 60-80%
- Cache hit rate: 40-60% for popular searches

### **After All Phases**
- Average query time: 50-200ms
- Database load: Reduced by 80-90%
- Cache hit rate: 70-90% for all searches

## 🔍 Query Analysis Tools

### **Enable SQL Query Logging**
```csharp
// In appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### **Performance Profiling**
```csharp
// Add query performance logging
public async Task<T> ExecuteWithTimingAsync<T>(Func<Task<T>> operation, string operationName)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await operation();
        stopwatch.Stop();
        
        if (stopwatch.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning("Slow query detected: {Operation} took {ElapsedMs}ms", 
                operationName, stopwatch.ElapsedMilliseconds);
        }
        
        return result;
    }
    finally
    {
        stopwatch.Stop();
    }
}
```