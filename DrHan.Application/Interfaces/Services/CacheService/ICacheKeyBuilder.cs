namespace DrHan.Application.Interfaces.Services.CacheService
{
    public interface ICacheKeyBuilder
    {
        string Build(params object[] segments);
        string BuildCollection<T>(params object[] segments);
        string BuildEntity<T>(object id, params object[] additionalSegments);
        string BuildPagedCollection<T>(int page, int size, params object[] segments);
        string BuildPattern(params object[] segments);
        string BuildUserKey(object userId, params object[] segments);
    }
}