using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DrHan.Application.Common.Interfaces;
using DrHan.Application.Common.Models;
using DrHan.Domain.Entities;
using DrHan.Domain.Interfaces;
using MediatR;

namespace DrHan.Application.Recipes.Queries.SearchRecipes
{
    public class SearchRecipesQueryHandler : IRequestHandler<SearchRecipesQuery, SearchRecipesResponse>
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IGeminiRecipeService _geminiRecipeService;
        private readonly ILogger<SearchRecipesQueryHandler> _logger;
        private readonly AppSettings _appSettings;

        public SearchRecipesQueryHandler(
            IRecipeRepository recipeRepository,
            IGeminiRecipeService geminiRecipeService,
            ILogger<SearchRecipesQueryHandler> logger,
            AppSettings appSettings)
        {
            _recipeRepository = recipeRepository;
            _geminiRecipeService = geminiRecipeService;
            _logger = logger;
            _appSettings = appSettings;
        }

        public async Task<SearchRecipesResponse> Handle(SearchRecipesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // First try to find recipes in the database
                var recipes = await _recipeRepository.SearchRecipesAsync(request.SearchTerm, cancellationToken);

                // If no recipes found and AI search is enabled, try AI service
                if (!recipes.Any() && _appSettings.EnableAISearch)
                {
                    _logger.LogInformation("No recipes found in database, trying AI service for term: {SearchTerm}", request.SearchTerm);
                    
                    var aiRecipes = await _geminiRecipeService.SearchRecipesAsync(request.SearchTerm, cancellationToken);
                    if (aiRecipes != null && aiRecipes.Any())
                    {
                        // Save AI recipes to database
                        foreach (var recipe in aiRecipes)
                        {
                            try
                            {
                                await _recipeRepository.AddRecipeAsync(recipe, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error saving AI recipe to database: {RecipeName}", recipe.Name);
                            }
                        }
                        recipes = aiRecipes;
                    }
                }

                return new SearchRecipesResponse
                {
                    Recipes = recipes.Select(r => new RecipeDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        Instructions = r.Instructions,
                        Ingredients = r.Ingredients.Select(i => new IngredientDto
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Quantity = i.Quantity,
                            Unit = i.Unit
                        }).ToList()
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching recipes for term: {SearchTerm}", request.SearchTerm);
                throw;
            }
        }
    }
} 