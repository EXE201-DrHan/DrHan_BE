using DrHan.Application.Interfaces.Services.CacheService;

namespace DrHan.Infrastructure.ExternalServices.CacheService
{
    // Generic cache key builder with configurable prefix
    public class CacheKeyBuilder 
    {
        private readonly string _appPrefix;

        public CacheKeyBuilder(string appPrefix = "app")
        {
            _appPrefix = appPrefix?.ToLowerInvariant() ?? "app";
        }

        // Build a key with segments
        public string Build(params object[] segments)
        {
            var normalizedSegments = segments
                .Where(s => s != null)
                .Select(NormalizeSegment)
                .Where(s => !string.IsNullOrEmpty(s));

            return $"{_appPrefix}:{string.Join(":", normalizedSegments)}";
        }

        // Build a key with a specific entity type
        public string BuildEntity<T>(object id, params object[] additionalSegments)
        {
            var entityName = typeof(T).Name.ToLowerInvariant();
            var allSegments = new[] { entityName, id }.Concat(additionalSegments);
            return Build(allSegments.ToArray());
        }

        // Build a collection key
        public string BuildCollection<T>(params object[] segments)
        {
            var entityName = typeof(T).Name.ToLowerInvariant();
            var allSegments = new[] { entityName }.Concat(segments);
            return Build(allSegments.ToArray());
        }

        // Build a paged collection key
        public string BuildPagedCollection<T>(int page, int size, params object[] segments)
        {
            return BuildCollection<T>(segments.Concat(new object[] { "page", page, "size", size }).ToArray());
        }

        // Build a user-specific key
        public string BuildUserKey(object userId, params object[] segments)
        {
            var allSegments = new[] { "user", userId }.Concat(segments);
            return Build(allSegments.ToArray());
        }

        // Build a pattern for bulk operations
        public string BuildPattern(params object[] segments)
        {
            return Build(segments.ToArray()) + ":*";
        }

        private string NormalizeSegment(object segment)
        {
            if (segment == null) return "null";

            return segment.ToString()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(":", "_")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(".", "_");
        }
    }




    public static class CacheKeyExtensions
    {
        public static string ToCacheKey<T>(this T entity, CacheKeyService cacheService, object id)
        {
            return cacheService.Entity<T>(id);
        }

        public static string ToCollectionCacheKey<T>(this IEnumerable<T> collection, CacheKeyService cacheService)
        {
            return cacheService.Collection<T>();
        }
    }
}
