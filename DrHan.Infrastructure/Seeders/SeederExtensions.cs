using DrHan.Domain.Entities;
using DrHan.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace DrHan.Infrastructure.Seeders
{
    public static class SeederExtensions
    {
        public static async Task SeedFromJsonAsync<T>(
            this ApplicationDbContext context, 
            string jsonFilePath, 
            Func<string, Task<List<T>>> parseFunction,
            ILogger? logger = null) where T : BaseEntity
        {
            var seeder = new GenericJsonSeeder<T>(context, jsonFilePath, parseFunction, logger);
            await seeder.SeedAsync();
        }

        public static GenericJsonSeeder<T> CreateSeeder<T>(
            this ApplicationDbContext context,
            string jsonFilePath,
            Func<string, Task<List<T>>> parseFunction,
            ILogger? logger = null) where T : BaseEntity
        {
            return new GenericJsonSeeder<T>(context, jsonFilePath, parseFunction, logger);
        }
    }
} 