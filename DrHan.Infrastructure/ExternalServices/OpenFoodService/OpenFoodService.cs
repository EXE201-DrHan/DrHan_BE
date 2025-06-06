using DrHan.Application.DTOs.OpenFoodFactProduct;
using DrHan.Domain.Entities;
using DrHan.Domain.Entities.Allergens;
using DrHan.Domain.Entities.FoodProducts;
using DrHan.Domain.Entities.Ingredients;
using DrHan.Infrastructure.Repositories;
using DrHan.Infrastructure.Repositories.HCP.Repository.GenericRepository;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using DrHan.Application.Interfaces.Repository;

namespace DrHan.Infrastructure.Services.OpenFoodService
{
    public class OpenFoodFactsService 
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OpenFoodFactsService> _logger;
        private const int BatchSize = 100;

        public OpenFoodFactsService(
            IUnitOfWork unitOfWork,
            ILogger<OpenFoodFactsService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<OpenFoodFactsProduct>> ReadCsvAsync(string filePath)
        {
            var products = new List<OpenFoodFactsProduct>();

            try
            {
                using var reader = new StreamReader(filePath);
                string? headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(headerLine))
                {
                    throw new InvalidOperationException("CSV file is empty or header is missing");
                }

                var headers = ParseCsvLine(headerLine);
                var headerMap = CreateHeaderMap(headers);

                string? line;
                int lineNumber = 1;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lineNumber++;

                    try
                    {
                        var values = ParseCsvLine(line);
                        var product = MapToOpenFoodFactsProduct(values, headerMap);

                        if (!string.IsNullOrWhiteSpace(product.ProductName))
                        {
                            products.Add(product);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing line {LineNumber}: {Line}", lineNumber, line);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file: {FilePath}", filePath);
                throw;
            }

            return products;
        }

        public async Task<int> ImportProductsAsync(IEnumerable<OpenFoodFactsProduct> products, CancellationToken cancellationToken = default)
        {
            int importedCount = 0;
            var batch = new List<FoodProduct>();

            try
            {
                foreach (var offProduct in products)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var foodProduct = await MapToFoodProductAsync(offProduct, cancellationToken);
                    if (foodProduct != null)
                    {
                        batch.Add(foodProduct);

                        if (batch.Count >= BatchSize)
                        {
                            await ProcessBatchAsync(batch, cancellationToken);
                            importedCount += batch.Count;
                            batch.Clear();

                            _logger.LogInformation("Processed {Count} products", importedCount);
                        }
                    }
                }

                // Process remaining items
                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch, cancellationToken);
                    importedCount += batch.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing products");
                throw;
            }

            return importedCount;
        }

        public async Task<int> ImportProductsFromCsvAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var products = await ReadCsvAsync(filePath);
            return await ImportProductsAsync(products, cancellationToken);
        }

        private async Task ProcessBatchAsync(List<FoodProduct> batch, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var productRepo = _unitOfWork.Repository<FoodProduct>();

                foreach (var product in batch)
                {
                    // Check if product already exists
                    var existingProduct = await productRepo.FindAsync(p => p.Barcode == product.Barcode && !string.IsNullOrEmpty(p.Barcode));

                    if (existingProduct == null)
                    {
                        await productRepo.AddAsync(product);
                    }
                    else
                    {
                        // Update existing product
                        UpdateExistingProduct(existingProduct, product);
                        productRepo.Update(existingProduct);
                    }
                }

                await _unitOfWork.CompleteAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        private async Task<FoodProduct?> MapToFoodProductAsync(OpenFoodFactsProduct offProduct, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(offProduct.ProductName))
                return null;

            var foodProduct = new FoodProduct
            {
                Barcode = CleanString(offProduct.Code),
                Name = CleanString(offProduct.ProductName) ?? "Unknown Product",
                Brand = CleanString(offProduct.Brands),
                Manufacturer = CleanString(offProduct.Manufacturer),
                ProductSize = CleanString(offProduct.Quantity),
                Category = ExtractMainCategory(offProduct.Categories),
                ServingSize = CleanString(offProduct.ServingSize),
                Description = BuildDescription(offProduct),
                DataSource = "openfoodfacts",
                DataQualityScore = CalculateDataQuality(offProduct),
            };

            // Parse calories
            if (int.TryParse(offProduct.EnergyKcal100g, out int calories))
            {
                foodProduct.CaloriesPerServing = calories;
            }

            // Process allergens
            await ProcessProductAllergensAsync(foodProduct, offProduct, cancellationToken);

            // Process ingredients
            await ProcessProductIngredientsAsync(foodProduct, offProduct, cancellationToken);

            // Add product image if available
            if (!string.IsNullOrWhiteSpace(offProduct.ImageUrl))
            {
                foodProduct.ProductImages.Add(new ProductImage
                {
                    ImageUrl = offProduct.ImageUrl,
                    ImageType = "product",
                    IsPrimary = true
                });
            }

            return foodProduct;
        }

        private async Task ProcessProductAllergensAsync(FoodProduct foodProduct, OpenFoodFactsProduct offProduct, CancellationToken cancellationToken)
        {
            var allergenRepo = _unitOfWork.Repository<Allergen>();
            var allergenNameRepo = _unitOfWork.Repository<AllergenName>();

            // Process "contains" allergens
            var containsAllergens = ParseAllergenString(offProduct.Allergens);
            foreach (var allergenName in containsAllergens)
            {
                var allergenNameEntity = await allergenNameRepo.FindAsync(an => an.Name.ToLower() == allergenName.ToLower());
                if (allergenNameEntity != null)
                {
                    foodProduct.ProductAllergens.Add(new ProductAllergen
                    {
                        AllergenId = allergenNameEntity.AllergenId,
                        AllergenType = "contains"
                    });
                }
            }

            // Process "may contain" allergens (traces)
            var tracesAllergens = ParseAllergenString(offProduct.Traces);
            foreach (var allergenName in tracesAllergens)
            {
                var allergenNameEntity = await allergenNameRepo.FindAsync(an => an.Name.ToLower() == allergenName.ToLower());
                if (allergenNameEntity != null && !foodProduct.ProductAllergens.Any(pa => pa.AllergenId == allergenNameEntity.AllergenId))
                {
                    foodProduct.ProductAllergens.Add(new ProductAllergen
                    {
                        AllergenId = allergenNameEntity.AllergenId,
                        AllergenType = "may_contain"
                    });
                }
            }
        }

        private async Task ProcessProductIngredientsAsync(FoodProduct foodProduct, OpenFoodFactsProduct offProduct, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(offProduct.Ingredients))
                return;

            var ingredientRepo = _unitOfWork.Repository<Ingredient>();
            var ingredientNameRepo = _unitOfWork.Repository<IngredientName>();
            var ingredientNames = ParseIngredientString(offProduct.Ingredients);
            int order = 1;

            foreach (var ingredientName in ingredientNames)
            {
                var normalizedName = NormalizeIngredientName(ingredientName);
                var ingredientNameEntity = await ingredientNameRepo.FindAsync(i => i.Name.ToLower() == normalizedName.ToLower());

                if (ingredientNameEntity == null)
                {
                    var ingredient = new Ingredient { Category = "unknown" };
                    await ingredientRepo.AddAsync(ingredient);
                    await _unitOfWork.CompleteAsync(cancellationToken);

                    ingredientNameEntity = new IngredientName
                    {
                        Name = normalizedName,
                        IngredientId = ingredient.Id
                    };
                    await ingredientNameRepo.AddAsync(ingredientNameEntity);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }

                foodProduct.ProductIngredients.Add(new ProductIngredient
                {
                    IngredientId = ingredientNameEntity.IngredientId,
                    OrderInList = order++
                });
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var regex = new Regex(@",(?=(?:[^""]*""[^""]*"")*[^""]*$)");
            var fields = regex.Split(line);

            foreach (var field in fields)
            {
                var cleanField = field.Trim();
                if (cleanField.StartsWith("\"") && cleanField.EndsWith("\""))
                {
                    cleanField = cleanField.Substring(1, cleanField.Length - 2);
                }
                result.Add(cleanField.Replace("\"\"", "\""));
            }

            return result.ToArray();
        }

        private static Dictionary<string, int> CreateHeaderMap(string[] headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Length; i++)
            {
                var header = headers[i].Trim().ToLower();

                // Map common OpenFoodFacts column names
                switch (header)
                {
                    case "code":
                    case "barcode":
                        map["code"] = i;
                        break;
                    case "product_name":
                    case "product_name_en":
                        map["product_name"] = i;
                        break;
                    case "brands":
                        map["brands"] = i;
                        break;
                    case "categories":
                    case "categories_en":
                        map["categories"] = i;
                        break;
                    case "ingredients_text":
                    case "ingredients_text_en":
                        map["ingredients"] = i;
                        break;
                    case "allergens":
                    case "allergens_en":
                        map["allergens"] = i;
                        break;
                    case "traces":
                    case "traces_en":
                        map["traces"] = i;
                        break;
                    case "quantity":
                        map["quantity"] = i;
                        break;
                    case "serving_size":
                        map["serving_size"] = i;
                        break;
                    case "energy-kcal_100g":
                    case "energy_kcal_100g":
                        map["energy_kcal_100g"] = i;
                        break;
                    case "manufacturing_places":
                    case "manufacturer":
                        map["manufacturer"] = i;
                        break;
                    case "image_url":
                    case "image_front_url":
                        map["image_url"] = i;
                        break;
                    case "nutrition_grades":
                    case "nutrition_grade_fr":
                        map["nutrition_grades"] = i;
                        break;
                    case "countries":
                    case "countries_en":
                        map["countries"] = i;
                        break;
                    case "created_t":
                        map["created_t"] = i;
                        break;
                    case "last_modified_t":
                        map["last_modified_t"] = i;
                        break;
                }
            }

            return map;
        }

        private static OpenFoodFactsProduct MapToOpenFoodFactsProduct(string[] values, Dictionary<string, int> headerMap)
        {
            return new OpenFoodFactsProduct
            {
                Code = GetValueSafely(values, headerMap, "code"),
                ProductName = GetValueSafely(values, headerMap, "product_name"),
                Brands = GetValueSafely(values, headerMap, "brands"),
                Categories = GetValueSafely(values, headerMap, "categories"),
                Ingredients = GetValueSafely(values, headerMap, "ingredients"),
                Allergens = GetValueSafely(values, headerMap, "allergens"),
                Traces = GetValueSafely(values, headerMap, "traces"),
                Quantity = GetValueSafely(values, headerMap, "quantity"),
                ServingSize = GetValueSafely(values, headerMap, "serving_size"),
                EnergyKcal100g = GetValueSafely(values, headerMap, "energy_kcal_100g"),
                Manufacturer = GetValueSafely(values, headerMap, "manufacturer"),
                ImageUrl = GetValueSafely(values, headerMap, "image_url"),
                NutritionGrades = GetValueSafely(values, headerMap, "nutrition_grades"),
                Countries = GetValueSafely(values, headerMap, "countries"),
                CreatedT = GetValueSafely(values, headerMap, "created_t"),
                LastModifiedT = GetValueSafely(values, headerMap, "last_modified_t")
            };
        }

        private static string? GetValueSafely(string[] values, Dictionary<string, int> headerMap, string key)
        {
            if (headerMap.TryGetValue(key, out int index) && index < values.Length)
            {
                var value = values[index];
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
            return null;
        }

        private static string? CleanString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            return input.Trim().Length > 0 ? input.Trim() : null;
        }

        private static List<string> ParseAllergenString(string? allergenString)
        {
            if (string.IsNullOrWhiteSpace(allergenString))
                return new List<string>();

            return allergenString
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim().ToLower())
                .Where(a => !string.IsNullOrEmpty(a))
                .Distinct()
                .ToList();
        }

        private static List<string> ParseIngredientString(string? ingredientString)
        {
            if (string.IsNullOrWhiteSpace(ingredientString))
                return new List<string>();

            return ingredientString
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => !string.IsNullOrEmpty(i))
                .Take(20) // Limit to first 20 ingredients
                .ToList();
        }

        private static string NormalizeIngredientName(string ingredientName)
        {
            // Remove parenthetical information and normalize
            var normalized = Regex.Replace(ingredientName, @"\([^)]*\)", "").Trim();
            return normalized.Length > 200 ? normalized.Substring(0, 200) : normalized;
        }

        private static string? ExtractMainCategory(string? categories)
        {
            if (string.IsNullOrWhiteSpace(categories))
                return null;

            var firstCategory = categories.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim();

            return firstCategory?.Length > 100 ? firstCategory.Substring(0, 100) : firstCategory;
        }

        private static string BuildDescription(OpenFoodFactsProduct product)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(product.Categories))
                parts.Add($"Categories: {product.Categories}");

            if (!string.IsNullOrWhiteSpace(product.Countries))
                parts.Add($"Countries: {product.Countries}");

            if (!string.IsNullOrWhiteSpace(product.NutritionGrades))
                parts.Add($"Nutrition Grade: {product.NutritionGrades}");

            return parts.Count > 0 ? string.Join("; ", parts) : null;
        }

        private static decimal CalculateDataQuality(OpenFoodFactsProduct product)
        {
            decimal score = 0.0m;

            // Basic data quality scoring
            if (!string.IsNullOrWhiteSpace(product.ProductName)) score += 0.2m;
            if (!string.IsNullOrWhiteSpace(product.Brands)) score += 0.1m;
            if (!string.IsNullOrWhiteSpace(product.Ingredients)) score += 0.3m;
            if (!string.IsNullOrWhiteSpace(product.Allergens)) score += 0.2m;
            if (!string.IsNullOrWhiteSpace(product.Categories)) score += 0.1m;
            if (!string.IsNullOrWhiteSpace(product.ImageUrl)) score += 0.1m;

            return Math.Min(1.0m, score);
        }

        private static void UpdateExistingProduct(FoodProduct existing, FoodProduct updated)
        {
            // Update basic properties if they're better in the new data
            if (!string.IsNullOrWhiteSpace(updated.Name) &&
                (string.IsNullOrWhiteSpace(existing.Name) || updated.Name.Length > existing.Name.Length))
            {
                existing.Name = updated.Name;
            }

            if (!string.IsNullOrWhiteSpace(updated.Brand) && string.IsNullOrWhiteSpace(existing.Brand))
                existing.Brand = updated.Brand;

            if (!string.IsNullOrWhiteSpace(updated.Manufacturer) && string.IsNullOrWhiteSpace(existing.Manufacturer))
                existing.Manufacturer = updated.Manufacturer;

            if (!string.IsNullOrWhiteSpace(updated.Category) && string.IsNullOrWhiteSpace(existing.Category))
                existing.Category = updated.Category;

            if (updated.CaloriesPerServing.HasValue && !existing.CaloriesPerServing.HasValue)
                existing.CaloriesPerServing = updated.CaloriesPerServing;

        }
    }
}
