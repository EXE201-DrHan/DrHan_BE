namespace DrHan.Application.Interfaces.Services.CacheService
{
    public interface ICacheKeyService
    {
        string Collection<T>();
        string CollectionByCategory<T>(string category);
        string Custom(params object[] segments);
        string Entity<T>(object id);
        string EntityPattern<T>();
        string EntityProperty<T>(object id, string property);
        string PagedCollection<T>(int page, int size);
        string PagedCollectionByCategory<T>(string category, int page, int size);
        string UserPattern();
        string UserPreferences(object userId);
        string UserProfile(object userId);
        string UserSetting(object userId, string setting);
    }
}