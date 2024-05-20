using System.Runtime.Caching;

namespace personal_daemon;

public interface ICachedArgsService
{
    string[] GetCachedArgs();
}

public class CachedArgsService : ICachedArgsService
{
    private readonly MemoryCache cache;

    public CachedArgsService(string[] args)
    {
        // Create a MemoryCache instance
        cache = MemoryCache.Default;

        // Define cache key and data
        string cacheKey = "args";
        string[] cachedData = args;

        // Add data to the cache with an expiration time of 5 minutes
        CacheItemPolicy cachePolicy = new CacheItemPolicy
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5)
        };

        cache.Add(cacheKey, cachedData, cachePolicy);
    }

    public string[] GetCachedArgs()
    {
        return (string[])cache.Get("args") ?? Array.Empty<string>();
    }
}