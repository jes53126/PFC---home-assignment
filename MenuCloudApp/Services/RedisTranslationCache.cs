using System.Collections.Concurrent;
using StackExchange.Redis;

namespace MenuCloudApp.Services;

public sealed class RedisTranslationCache : ITranslationCache
{
    private readonly IConnectionMultiplexer? _redis;
    private static readonly ConcurrentDictionary<string, string> LocalFallback = new();

    public RedisTranslationCache(IServiceProvider services)
    {
        _redis = services.GetService<IConnectionMultiplexer>();
    }

    public async Task<string?> GetAsync(string menuId, string itemName, string language)
    {
        var key = Key(menuId, itemName, language);
        if (_redis is null)
        {
            return LocalFallback.TryGetValue(key, out var value) ? value : null;
        }

        return await _redis.GetDatabase().StringGetAsync(key);
    }

    public async Task SetAsync(string menuId, string itemName, string language, string translatedText)
    {
        var key = Key(menuId, itemName, language);
        if (_redis is null)
        {
            LocalFallback[key] = translatedText;
            return;
        }

        await _redis.GetDatabase().StringSetAsync(key, translatedText, TimeSpan.FromDays(30));
    }

    private static string Key(string menuId, string itemName, string language)
        => $"translation:{menuId}:{language}:{itemName.Trim().ToLowerInvariant()}";
}
