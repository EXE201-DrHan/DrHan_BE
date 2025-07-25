# Database Optimization Strategies for Recipe Search

## 1. 🚀 Indexing Strategy

### Core Search Indexes
```sql
-- Full-text search index for recipe names and descriptions
CREATE FULLTEXT INDEX IX_Recipes_FullText 
ON Recipes (Name, Description) 
KEY INDEX IX_Recipes_FullText_Key

-- Composite index for common filter combinations
CREATE INDEX IX_Recipes_Search_Composite 
ON Recipes (CuisineType, MealType, DifficultyLevel, IsPublic, CreateAt)

-- Specialized index for AI-generated recipes
CREATE INDEX IX_Recipes_AI_Generated 
ON Recipes (OriginalAuthor, CreateAt) 
WHERE OriginalAuthor = 'AI Generated'

-- Ingredient search optimization
CREATE INDEX IX_RecipeIngredients_Search 
ON RecipeIngredients (IngredientName, RecipeId)
```

### Search Performance Indexes
```sql
-- Time-based filtering
CREATE INDEX IX_Recipes_Time_Filters 
ON Recipes (PrepTimeMinutes, CookTimeMinutes, Servings)

-- Rating and popularity
CREATE INDEX IX_Recipes_Popularity 
ON Recipes (AverageRating, LikesCount, CreateAt)

-- Allergen filtering (covering index)
CREATE INDEX IX_RecipeAllergens_Covering 
ON RecipeAllergens (AllergenType, RecipeId) 
INCLUDE (CreateAt)
```

## 2. 🔍 Query Optimization

### Optimized Search Query Pattern
```csharp
// Use compiled queries for frequently executed searches
private static readonly Func<RecipeDbContext, string, int, int, IAsyncEnumerable<Recipe>>
    CompiledSearchQuery = EF.CompileAsyncQuery(
        (RecipeDbContext context, string searchTerm, int skip, int take) =>
            context.Recipes
                .Where(r => EF.Functions.Contains(r.Name, searchTerm) || 
                           EF.Functions.Contains(r.Description, searchTerm))
                .OrderByDescending(r => r.CreateAt)
                .Skip(skip)
                .Take(take));
```

### Smart Query Execution
```csharp
// Use query splitting for complex includes
var recipes = await context.Recipes
    .AsSplitQuery() // Prevents cartesian explosion
    .Include(r => r.RecipeIngredients)
        .ThenInclude(ri => ri.Ingredient)
    .Include(r => r.RecipeInstructions)
    .Where(/* filters */)
    .ToListAsync();
```

## 3. 📊 Caching Strategy

### Multi-Level Caching
```csharp
// L1: In-Memory Cache (fastest, smallest)
private readonly IMemoryCache _l1Cache;

// L2: Distributed Cache (Redis) for shared results
private readonly IDistributedCache _l2Cache;

// L3: Database with optimized queries
```

### Cache Key Strategy
```csharp
public string GenerateOptimizedCacheKey(RecipeSearchDto search)
{
    var keyBuilder = new StringBuilder();
    keyBuilder.Append($"recipes");
    keyBuilder.Append($":term:{search.SearchTerm?.ToLowerInvariant() ?? "all"}");
    keyBuilder.Append($":cuisine:{search.CuisineType ?? "any"}");
    keyBuilder.Append($":meal:{search.MealType ?? "any"}");
    keyBuilder.Append($":page:{search.Page}");
    keyBuilder.Append($":size:{search.PageSize}");
    
    // Hash long keys for performance
    var key = keyBuilder.ToString();
    return key.Length > 100 ? ComputeHash(key) : key;
}
```

## 4. 🏗️ Database Schema Optimizations

### Denormalization for Search Performance
```sql
-- Add computed columns for faster searches
ALTER TABLE Recipes 
ADD SearchVector AS CONCAT(Name, ' ', Description, ' ', CuisineType, ' ', MealType)

-- Add materialized search index
CREATE INDEX IX_Recipes_SearchVector ON Recipes (SearchVector)
```

### Partitioning Strategy
```sql
-- Partition by creation date for time-based queries
CREATE PARTITION FUNCTION RecipeDatePartition (datetime2)
AS RANGE RIGHT FOR VALUES 
('2024-01-01', '2024-04-01', '2024-07-01', '2024-10-01')

CREATE PARTITION SCHEME RecipeDateScheme
AS PARTITION RecipeDatePartition
ALL TO ([PRIMARY])
```

## 5. 🔄 Background Optimization Tasks

### Automatic Index Maintenance
```csharp
public class DatabaseOptimizationService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await OptimizeIndexes();
            await UpdateStatistics();
            await CleanupOldCache();
            
            // Run once per day
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
    
    private async Task OptimizeIndexes()
    {
        // Rebuild fragmented indexes
        await _context.Database.ExecuteSqlRawAsync(@"
            DECLARE @sql NVARCHAR(MAX) = '';
            SELECT @sql = @sql + 'ALTER INDEX ' + i.name + ' ON ' + t.name + ' REBUILD;' + CHAR(13)
            FROM sys.indexes i
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            WHERE i.avg_fragmentation_in_percent > 30
            AND i.name IS NOT NULL;
            EXEC sp_executesql @sql;
        ");
    }
}
```

## 6. 📈 Performance Monitoring

### Query Performance Tracking
```csharp
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryPerformanceInterceptor> _logger;
    
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var duration = eventData.Duration.TotalMilliseconds;
        
        if (duration > 1000) // Log slow queries
        {
            _logger.LogWarning("Slow query detected: {Duration}ms - {Query}", 
                duration, command.CommandText);
        }
        
        // Track performance metrics
        Metrics.RecordQueryDuration(duration);
        
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
```

## 7. 🎯 Search-Specific Optimizations

### Elasticsearch Integration (Advanced)
```csharp
public class ElasticsearchRecipeService : IAdvancedSearchService
{
    private readonly IElasticClient _elasticClient;
    
    public async Task<SearchResponse<RecipeDocument>> SearchAsync(
        RecipeSearchDto searchDto)
    {
        var searchRequest = new SearchRequest<RecipeDocument>
        {
            Query = new BoolQuery
            {
                Must = new QueryContainer[]
                {
                    new MultiMatchQuery
                    {
                        Query = searchDto.SearchTerm,
                        Fields = new Field[] { "name^3", "description^2", "ingredients" },
                        Fuzziness = Fuzziness.Auto
                    }
                },
                Filter = BuildElasticsearchFilters(searchDto)
            },
            Sort = BuildElasticsearchSort(searchDto),
            Size = searchDto.PageSize,
            From = (searchDto.Page - 1) * searchDto.PageSize,
            Highlight = new Highlight
            {
                Fields = new Dictionary<Field, IHighlightField>
                {
                    { "name", new HighlightField() },
                    { "description", new HighlightField() }
                }
            }
        };
        
        return await _elasticClient.SearchAsync<RecipeDocument>(searchRequest);
    }
}
```

## 8. 🔧 Configuration Optimizations

### Connection Pool Optimization
```csharp
services.AddDbContext<RecipeDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        sqlOptions.EnableRetryOnFailure(3);
    });
    
    // Optimize for read-heavy workloads
    options.EnableSensitiveDataLogging(false);
    options.EnableServiceProviderCaching();
    options.EnableDetailedErrors(false);
});

// Configure connection pooling
services.AddDbContextPool<RecipeDbContext>(options => 
{
    // Configure options
}, poolSize: 128); // Optimize pool size based on load
```

## 9. 📊 Performance Benchmarks

### Target Metrics
- **Database Query Time**: < 50ms for simple searches
- **Complex Search Time**: < 200ms with filters
- **Cache Hit Ratio**: > 80% for popular searches
- **AI Integration Time**: < 2 seconds total
- **Memory Usage**: < 100MB per search operation

### Monitoring Queries
```sql
-- Monitor query performance
SELECT 
    query_hash,
    query_plan_hash,
    total_elapsed_time / execution_count as avg_duration_ms,
    execution_count,
    total_logical_reads / execution_count as avg_logical_reads
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
WHERE st.text LIKE '%Recipes%'
ORDER BY avg_duration_ms DESC;
```