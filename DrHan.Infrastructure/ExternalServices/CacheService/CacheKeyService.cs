using DrHan.Application.Interfaces.Services.CacheService;

namespace DrHan.Infrastructure.ExternalServices.CacheService
{
    public class CacheKeyService : ICacheKeyService
    {
        private readonly CacheKeyBuilder _builder;

        public CacheKeyService(string appPrefix = "app")
        {
            _builder = new CacheKeyBuilder(appPrefix);
        }

        // Generic entity operations
        public string Entity<T>(object id) => _builder.BuildEntity<T>(id);
        public string EntityProperty<T>(object id, string property) => _builder.BuildEntity<T>(id, property);
        public string Collection<T>() => _builder.BuildCollection<T>();
        public string CollectionByCategory<T>(string category) => _builder.BuildCollection<T>("category", category);
        public string PagedCollection<T>(int page, int size) => _builder.BuildPagedCollection<T>(page, size);
        public string PagedCollectionByCategory<T>(string category, int page, int size)
            => _builder.BuildPagedCollection<T>(page, size, "category", category);

        // User operations
        public string UserProfile(object userId) => _builder.BuildUserKey(userId, "profile");
        public string UserPreferences(object userId) => _builder.BuildUserKey(userId, "preferences");
        public string UserSetting(object userId, string setting) => _builder.BuildUserKey(userId, "settings", setting);

        // Pattern operations
        public string EntityPattern<T>() => _builder.BuildPattern(typeof(T).Name.ToLowerInvariant());
        public string UserPattern() => _builder.BuildPattern("user");

        // Custom key building
        public string Custom(params object[] segments) => _builder.Build(segments);
    }

}
