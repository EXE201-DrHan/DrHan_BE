using DrHan.Application.DTOs.OpenFoodFactProduct;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DrHan.Infrastructure.Services.CsvReader
{
    public class CsvParser
    {
        private readonly ILogger<CsvParser> _logger;

        public CsvParser(ILogger<CsvParser> logger)
        {
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
    }
}
