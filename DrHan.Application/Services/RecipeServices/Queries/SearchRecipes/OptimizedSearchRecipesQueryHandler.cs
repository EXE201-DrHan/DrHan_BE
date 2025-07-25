using AutoMapper;
using DrHan.Application.Commons;
using DrHan.Application.DTOs.Gemini;
using DrHan.Application.DTOs.Recipes;
using DrHan.Application.Interfaces.Repository;
using DrHan.Application.Interfaces.Services;
using DrHan.Application.StaticQuery;
using DrHan.Domain.Entities.Recipes;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DrHan.Application.Services.RecipeServices.Queries.SearchRecipes;

/// <summary>
/// Optimized search handler with advanced caching, parallel processing, and intelligent AI integration
/// </summary>
public class OptimizedSearchRecipesQueryHandler : IRequestHandler<SearchRecipesQuery, AppResponse<IPaginatedList<RecipeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IGeminiRecipeService _geminiService;
    private readonly IRecipeSearchCacheService _cacheService;
    private readonly IRecipePersistenceService _persistenceService;
    private readonly ILogger<OptimizedSearchRecipesQueryHandler> _logger;
    private readonly IMemoryCache _memoryCache;

    // Optimization settings
    private const int MIN_RECIPES_THRESHOLD = 8; // Reduced threshold for better user experience
    private const int MAX_AI_RECIPES_REQUEST = 12; // Request more to account for filtering
    private const int CACHE_EXPIRY_MINUTES = 30; // Aggressive caching for performance
    private const int PARALLEL_SEARCH_THRESHOLD = 3; // Trigger parallel operations when needed

    // Circuit breaker for AI service
    private static readonly ConcurrentDictionary<string, DateTime> _aiServiceFailures = new();
    private const int AI_CIRCUIT_BREAKER_MINUTES = 5;

    public OptimizedSearchRecipesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IGeminiRecipeService geminiService,
        IRecipeSearchCacheService cacheService,
        IRecipePersistenceService persistenceService,
        IMemoryCache memoryCache,
        ILogger<OptimizedSearchRecipesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AppResponse<IPaginatedList<RecipeDto>>> Handle(
        SearchRecipesQuery request,
        CancellationToken cancellationToken)
    {
        var searchDto = request.SearchDto;
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("🔍 Starting optimized search: '{SearchTerm}' | Page: {Page} | Size: {PageSize}", 
            searchDto.SearchTerm, searchDto.Page, searchDto.PageSize);

        try
        {
            // Step 1: Check cache first (fastest path)
            var cacheKey = _cacheService.GenerateSearchCacheKey(searchDto);
            var cachedResults = await _cacheService.GetCachedSearchResultsAsync(cacheKey);
            
            if (cachedResults != null)
            {
                _logger.LogInformation("⚡ Cache hit for search '{SearchTerm}' in {ElapsedMs}ms", 
                    searchDto.SearchTerm, stopwatch.ElapsedMilliseconds);
                return new AppResponse<IPaginatedList<RecipeDto>>()
                    .SetSuccessResponse(cachedResults, "Thành công", "Kết quả từ cache");
            }

            // Step 2: Parallel database and AI preparation
            var dbTask = GetRecipesFromDatabaseAsync(searchDto, cancellationToken);
            var shouldPrepareAI = ShouldPrepareAISearch(searchDto);
            var aiPreparationTask = shouldPrepareAI ? PrepareAISearchAsync(searchDto, cancellationToken) : Task.FromResult<List<GeminiRecipeResponseDto>?>(null);

            // Wait for database results first (usually fastest)
            var dbRecipes = await dbTask;
            
            _logger.LogInformation("📊 Database search completed: {Count} recipes found in {ElapsedMs}ms", 
                dbRecipes.Items.Count, stopwatch.ElapsedMilliseconds);

            // Step 3: Intelligent AI augmentation decision
            var needsAIAugmentation = ShouldUseAIAugmentation(dbRecipes, searchDto);
            
            if (needsAIAugmentation)
            {
                var aiRecipes = await aiPreparationTask;
                if (aiRecipes?.Any() == true)
                {
                    _logger.LogInformation("🤖 AI augmentation successful: {Count} recipes in {ElapsedMs}ms total", 
                        aiRecipes.Count, stopwatch.ElapsedMilliseconds);
                    
                    var result = CreateOptimizedResponseWithAI(dbRecipes, aiRecipes, searchDto);
                    
                    // Async cache and persistence (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        await CacheAndPersistAsync(cacheKey, result, aiRecipes, searchDto);
                    }, cancellationToken);
                    
                    return result;
                }
            }

            // Step 4: Return database-only results
            var dbOnlyResult = CreateOptimizedResponse(dbRecipes, searchDto);
            
            // Cache database results for future requests
            _ = Task.Run(async () =>
            {
                await _cacheService.CacheSearchResultsAsync(cacheKey, dbOnlyResult.Data!, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
            }, cancellationToken);

            _logger.LogInformation("✅ Search completed in {ElapsedMs}ms: {Count} recipes returned", 
                stopwatch.ElapsedMilliseconds, dbRecipes.Items.Count);

            return dbOnlyResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Search failed for '{SearchTerm}' after {ElapsedMs}ms", 
                searchDto.SearchTerm, stopwatch.ElapsedMilliseconds);
            return new AppResponse<IPaginatedList<RecipeDto>>()
                .SetErrorResponse("Lỗi", "Đã xảy ra lỗi khi tìm kiếm công thức nấu ăn");
        }
    }

    /// <summary>
    /// Enhanced database search with optimized queries
    /// </summary>
    private async Task<IPaginatedList<Recipe>> GetRecipesFromDatabaseAsync(
        RecipeSearchDto searchDto, 
        CancellationToken cancellationToken)
    {
        var paginationRequest = new PaginationRequest(searchDto.Page, searchDto.PageSize);

        // Use memory cache for repeated identical queries within short timespan
        var queryCacheKey = $"db_query_{searchDto.GetHashCode()}";
        if (_memoryCache.TryGetValue(queryCacheKey, out IPaginatedList<Recipe>? cachedDbResult))
        {
            return cachedDbResult!;
        }

        var result = await _unitOfWork.Repository<Recipe>().ListAsyncWithPaginated(
            filter: RecipeSearchQuery.BuildFilter(searchDto),
            orderBy: RecipeSearchQuery.BuildOrderBy(searchDto),
            includeProperties: RecipeSearchQuery.BuildSearchIncludes(),
            pagination: paginationRequest,
            cancellationToken: cancellationToken);

        // Cache for 2 minutes to handle rapid repeated requests
        _memoryCache.Set(queryCacheKey, result, TimeSpan.FromMinutes(2));

        return result;
    }

    /// <summary>
    /// Intelligent decision on whether to prepare AI search
    /// </summary>
    private bool ShouldPrepareAISearch(RecipeSearchDto searchDto)
    {
        // Don't prepare AI for very common searches that likely have good database coverage
        var commonTerms = new[] { "cơm", "phở", "bánh", "canh", "soup" };
        if (commonTerms.Any(term => searchDto.SearchTerm?.Contains(term, StringComparison.OrdinalIgnoreCase) == true))
        {
            return searchDto.Page == 1; // Only for first page of common terms
        }

        // Check circuit breaker
        var circuitKey = $"ai_circuit_{searchDto.SearchTerm?.ToLower()}";
        if (_aiServiceFailures.TryGetValue(circuitKey, out var lastFailure))
        {
            if (DateTime.UtcNow.Subtract(lastFailure).TotalMinutes < AI_CIRCUIT_BREAKER_MINUTES)
            {
                return false; // Circuit is open
            }
            _aiServiceFailures.TryRemove(circuitKey, out _); // Reset circuit
        }

        return true;
    }

    /// <summary>
    /// Prepare AI search asynchronously
    /// </summary>
    private async Task<List<GeminiRecipeResponseDto>?> PrepareAISearchAsync(
        RecipeSearchDto searchDto, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if we have cached AI recipes first
            var searchContext = GenerateSearchContext(searchDto);
            var cachedAI = await _cacheService.GetCachedAIRecipesAsync(searchContext);
            if (cachedAI?.Any() == true)
            {
                return cachedAI;
            }

            // Generate new AI recipes
            var geminiRequest = CreateOptimizedGeminiRequest(searchDto);
            var aiRecipes = await _geminiService.SearchRecipesAsync(geminiRequest);
            
            if (aiRecipes?.Any() == true)
            {
                // Cache AI recipes for reuse
                await _cacheService.CacheAIRecipesAsync(searchContext, aiRecipes, TimeSpan.FromHours(2));
            }

            return aiRecipes;
        }
        catch (Exception ex)
        {
            // Add to circuit breaker
            var circuitKey = $"ai_circuit_{searchDto.SearchTerm?.ToLower()}";
            _aiServiceFailures.TryAdd(circuitKey, DateTime.UtcNow);
            
            _logger.LogWarning(ex, "AI search preparation failed for '{SearchTerm}'", searchDto.SearchTerm);
            return null;
        }
    }

    /// <summary>
    /// Smart decision on whether to use AI augmentation
    /// </summary>
    private bool ShouldUseAIAugmentation(IPaginatedList<Recipe> dbRecipes, RecipeSearchDto searchDto)
    {
        // Use AI if we have fewer than threshold
        if (dbRecipes.Items.Count < MIN_RECIPES_THRESHOLD)
            return true;

        // For first page, use AI if we have less than page size to provide more variety
        if (searchDto.Page == 1 && dbRecipes.Items.Count < searchDto.PageSize)
            return true;

        return false;
    }

    /// <summary>
    /// Create optimized Gemini request with smart parameters
    /// </summary>
    private GeminiRecipeRequestDto CreateOptimizedGeminiRequest(RecipeSearchDto searchDto)
    {
        return new GeminiRecipeRequestDto
        {
            SearchQuery = searchDto.SearchTerm ?? "popular recipes",
            CuisineType = searchDto.CuisineType,
            MealType = searchDto.MealType,
            DifficultyLevel = searchDto.DifficultyLevel,
            MaxPrepTime = searchDto.MaxPrepTime,
            MaxCookTime = searchDto.MaxCookTime,
            Servings = searchDto.MinServings ?? 4,
            ExcludeAllergens = searchDto.ExcludeAllergens,
            Count = MAX_AI_RECIPES_REQUEST, // Request more to allow for filtering
            IncludeImage = false // Optimize by excluding images initially
        };
    }

    /// <summary>
    /// Generate search context for caching
    /// </summary>
    private string GenerateSearchContext(RecipeSearchDto searchDto)
    {
        return $"{searchDto.SearchTerm}|{searchDto.CuisineType}|{searchDto.MealType}|{searchDto.DifficultyLevel}";
    }

    /// <summary>
    /// Create optimized response with AI recipes
    /// </summary>
    private AppResponse<IPaginatedList<RecipeDto>> CreateOptimizedResponseWithAI(
        IPaginatedList<Recipe> dbRecipes,
        List<GeminiRecipeResponseDto> aiRecipes,
        RecipeSearchDto searchDto)
    {
        // Convert database recipes
        var dbDtos = _mapper.Map<List<RecipeDto>>(dbRecipes.Items);

        // Convert AI recipes with optimized mapping
        var aiDtos = aiRecipes.Take(MIN_RECIPES_THRESHOLD - dbRecipes.Items.Count)
            .Select((ai, index) => new RecipeDto
            {
                Id = -(index + 1), // Negative IDs for AI recipes
                BusinessId = Guid.NewGuid(),
                Name = ai.Name,
                Description = ai.Description?.Length > 200 ? ai.Description[..200] + "..." : ai.Description,
                CuisineType = ai.CuisineType,
                MealType = ai.MealType,
                PrepTimeMinutes = ai.PrepTimeMinutes,
                CookTimeMinutes = ai.CookTimeMinutes,
                Servings = ai.Servings,
                DifficultyLevel = ai.DifficultyLevel,
                IsCustom = false,
                IsPublic = true,
                SourceUrl = "AI Generated",
                OriginalAuthor = "AI Generated",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            }).ToList();

        var allRecipes = dbDtos.Concat(aiDtos).ToList();

        var result = PaginatedList<RecipeDto>.Create(
            allRecipes,
            searchDto.Page,
            searchDto.PageSize,
            dbRecipes.TotalCount + aiDtos.Count);

        return new AppResponse<IPaginatedList<RecipeDto>>()
            .SetSuccessResponse(result, "Thành công", "Kết quả được tăng cường bởi AI");
    }

    /// <summary>
    /// Create optimized response for database-only results
    /// </summary>
    private AppResponse<IPaginatedList<RecipeDto>> CreateOptimizedResponse(
        IPaginatedList<Recipe> dbRecipes,
        RecipeSearchDto searchDto)
    {
        var recipeDtos = _mapper.Map<List<RecipeDto>>(dbRecipes.Items);
        var result = PaginatedList<RecipeDto>.Create(
            recipeDtos,
            searchDto.Page,
            searchDto.PageSize,
            dbRecipes.TotalCount);

        return new AppResponse<IPaginatedList<RecipeDto>>()
            .SetSuccessResponse(result, "Thành công");
    }

    /// <summary>
    /// Handle caching and persistence asynchronously
    /// </summary>
    private async Task CacheAndPersistAsync(
        string cacheKey,
        AppResponse<IPaginatedList<RecipeDto>> result,
        List<GeminiRecipeResponseDto> aiRecipes,
        RecipeSearchDto searchDto)
    {
        try
        {
            // Cache the complete result
            await _cacheService.CacheSearchResultsAsync(cacheKey, result.Data!, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));

            // Queue for persistence
            var searchContext = GenerateSearchContext(searchDto);
            await _persistenceService.QueueRecipesForPersistenceAsync(aiRecipes, searchContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache and persist AI recipes");
        }
    }
}