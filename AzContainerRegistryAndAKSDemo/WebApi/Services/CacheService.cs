
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace WebApi.Services;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    // private readonly IDistributedCache _distributedCache;

    public CacheService(IMemoryCache memoryCache 
           //IDistributedCache distributedCache
          )
    {
        _memoryCache = memoryCache;
        //_distributedCache = distributedCache;
    }


    public T GetData<T>(string key)
    {
        // check in In-Memory First
        if(_memoryCache.TryGetValue(key, out T data))
        {
            return data;
        }

        //check in Distributed Cache
        //var cacheData = _distributedCache.GetString(key);
        //if (!string.IsNullOrEmpty(cacheData))
        //{
        //    return JsonSerializer.Deserialize<T>(cacheData);
        //}
        return default;
    }

    public bool SetData<T>(string key, T value, DateTimeOffset expirationTime)
    {
        bool result = true;
        try
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                // set in-memory cache
                _memoryCache.Set(key, value, expirationTime);

                // Set in distributed cache
                var serializedData = JsonSerializer.Serialize(value);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = expirationTime,
                };
                // _distributedCache.SetString(key, serializedData, cacheOptions);
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            throw;
        }
        return result;
    }


    public void RemoveData(string key)
    {
        // Remove from in-memory cache
        _memoryCache.Remove(key);

        // Remove from distributed cache
        // _distributedCache.Remove(key);
    }
}
