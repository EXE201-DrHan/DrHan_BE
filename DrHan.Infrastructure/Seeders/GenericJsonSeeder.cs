using DrHan.Domain.Entities;
using DrHan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DrHan.Infrastructure.Seeders
{
    public class GenericJsonSeeder<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string _absoluteFilePathJson;
        private readonly ILogger? _logger;
        private readonly Func<string, Task<List<T>>> _parseJsonToObject;

        public GenericJsonSeeder(
            ApplicationDbContext dbContext, 
            string absoluteFilePathJson, 
            Func<string, Task<List<T>>> parseJsonToObject,
            ILogger? logger = null)
        {
            _dbContext = dbContext;
            _absoluteFilePathJson = absoluteFilePathJson;
            _parseJsonToObject = parseJsonToObject;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                var entityName = typeof(T).Name;
                _logger?.LogInformation($"Starting to seed {entityName}...");

                // Check file exists first
                if (string.IsNullOrEmpty(_absoluteFilePathJson)) 
                    throw new FileNotFoundException($"File path is null or empty for {entityName}");

                if (!File.Exists(_absoluteFilePathJson))
                    throw new FileNotFoundException($"JSON file not found: {_absoluteFilePathJson}");

                // Seed data based on entity
                if (await _dbContext.Database.CanConnectAsync())
                {
                    if (!await _dbContext.Set<T>().AnyAsync())
                    {
                        _logger?.LogInformation($"No existing {entityName} data found. Proceeding with seeding...");
                        
                        var entities = await ParseJsonToObject();
                        
                        if (entities?.Any() == true)
                        {
                            await _dbContext.Set<T>().AddRangeAsync(entities);
                            await _dbContext.SaveChangesAsync();
                            _logger?.LogInformation($"Successfully seeded {entities.Count} {entityName} records.");
                        }
                        else
                        {
                            _logger?.LogWarning($"No {entityName} data found in JSON file.");
                        }
                    }
                    else
                    {
                        _logger?.LogInformation($"{entityName} data already exists. Skipping seeding.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Cannot connect to database");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error occurred while seeding {typeof(T).Name}");
                throw;
            }
        }

        private async Task<List<T>> ParseJsonToObject()
        {
            var jsonContent = await File.ReadAllTextAsync(_absoluteFilePathJson);
            return await _parseJsonToObject(jsonContent);
        }
    }
} 