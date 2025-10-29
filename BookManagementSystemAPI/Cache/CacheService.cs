
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using StackExchange.Redis;

public class CacheService : ICacheService
{
    private readonly IDatabase? _redisDb;

    public CacheService(IConnectionMultiplexer connectionMultiplexer)
    {
        _redisDb = connectionMultiplexer.GetDatabase();
    }
    public T GetData<T>(string key)
    {
        if (_redisDb == null)
        {
            return default;
        }

        var value = _redisDb.StringGet(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value);
    }

    /// <summary>
    /// This is more for force refresh.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public object RemoveData(string key)
    {
        var isKeyExist = _redisDb != null && _redisDb.KeyExists(key);
        return isKeyExist && _redisDb.KeyDelete(key);
    }

    public bool SetData<T>(string key, T value, DateTimeOffset expirationTime)
    {
        var expiryTime = expirationTime.DateTime.Subtract(DateTime.Now);

        var isSet = _redisDb != null && _redisDb.StringSet(key, JsonSerializer.Serialize(value), expiryTime);
        return isSet;
    }
}