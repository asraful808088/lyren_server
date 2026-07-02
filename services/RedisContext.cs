using StackExchange.Redis;
using System.Text.Json;

namespace Eservice.RedisServices
{


    public class RedisService
{
    private readonly ConnectionMultiplexer _redis;
    public IDatabase Db { get; }
    public IConnectionMultiplexer Multiplexer => _redis;

    public RedisService(IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
            throw new Exception("Redis connection string not found.");

        _redis = ConnectionMultiplexer.Connect(connectionString);
        Db = _redis.GetDatabase();
        Console.WriteLine("✅ Redis Connected");
    }
}
    public interface IRedisRepository
    {
        // Basic Key-Value (String)
        Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetStringAsync(string key);

        // Generic Object (JSON serialized)
        Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T?> GetObjectAsync<T>(string key);

        // Key management
        Task<bool> KeyExistsAsync(string key);
        Task<bool> RemoveKeyAsync(string key);
        Task<bool> RemoveKeysAsync(IEnumerable<string> keys);
        Task<bool> ExpireKeyAsync(string key, TimeSpan expiry);
        Task<TimeSpan?> GetTtlAsync(string key);
        Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern);

        // Hash operations
        Task<bool> HashSetAsync(string key, string field, string value);
        Task<bool> HashSetObjectAsync<T>(string key, string field, T value);
        Task<string?> HashGetAsync(string key, string field);
        Task<T?> HashGetObjectAsync<T>(string key, string field);
        Task<Dictionary<string, string>> HashGetAllAsync(string key);
        Task<bool> HashDeleteAsync(string key, string field);
        Task<bool> HashExistsAsync(string key, string field);

        // List operations
        Task<long> ListLeftPushAsync(string key, string value);
        Task<long> ListRightPushAsync(string key, string value);
        Task<string?> ListLeftPopAsync(string key);
        Task<string?> ListRightPopAsync(string key);
        Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1);
        Task<long> ListLengthAsync(string key);

        // Set operations
        Task<bool> SetAddAsync(string key, string value);
        Task<bool> SetRemoveAsync(string key, string value);
        Task<bool> SetContainsAsync(string key, string value);
        Task<List<string>> SetMembersAsync(string key);
        Task<long> SetLengthAsync(string key);

        // Sorted Set operations
        Task<bool> SortedSetAddAsync(string key, string member, double score);
        Task<bool> SortedSetRemoveAsync(string key, string member);
        Task<List<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, bool descending = false);
        Task<double?> SortedSetScoreAsync(string key, string member);
        Task<long> SortedSetLengthAsync(string key);

        // Increment/Decrement
        Task<long> IncrementAsync(string key, long value = 1);
        Task<long> DecrementAsync(string key, long value = 1);

        // Pub/Sub
        Task PublishAsync(string channel, string message);
        Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler);

        // Transaction example
        Task<bool> SetMultipleAsync(Dictionary<string, string> keyValues);
    }

    public class RedisRepository : IRedisRepository
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisRepository(RedisService redisService, IConnectionMultiplexer connectionMultiplexer)
        {
            _db = redisService.Db;
            _connectionMultiplexer = connectionMultiplexer;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // ---------------- STRING ----------------

       public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
{
    return await _db.StringSetAsync(key, value, expiry.HasValue ? (Expiration)expiry.Value : default);
}
        public async Task<string?> GetStringAsync(string key)
        {
            var value = await _db.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
{
    var json = JsonSerializer.Serialize(value, _jsonOptions);
    return await _db.StringSetAsync(key, json, expiry, When.Always, CommandFlags.None);
}

        public async Task<T?> GetObjectAsync<T>(string key)
{
    var value = await _db.StringGetAsync(key);
    if (!value.HasValue) return default;

    string json = value.ToString();
    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
}

        // ---------------- KEY MANAGEMENT ----------------

        public async Task<bool> KeyExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }

        public async Task<bool> RemoveKeyAsync(string key)
        {
            return await _db.KeyDeleteAsync(key);
        }

        public async Task<bool> RemoveKeysAsync(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var deletedCount = await _db.KeyDeleteAsync(redisKeys);
            return deletedCount > 0;
        }

        public async Task<bool> ExpireKeyAsync(string key, TimeSpan expiry)
        {
            return await _db.KeyExpireAsync(key, expiry);
        }

        public async Task<TimeSpan?> GetTtlAsync(string key)
        {
            return await _db.KeyTimeToLiveAsync(key);
        }














        public async Task<T?> HashGetObjectAsync<T>(string key, string field)
{
    var value = await _db.HashGetAsync(key, field);
    if (!value.HasValue) return default;

    string json = value.ToString();
    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
}








        public async Task<IEnumerable<string>> GetKeysByPatternAsync(string pattern)
        {
            var endpoints = _connectionMultiplexer.GetEndPoints();
            var server = _connectionMultiplexer.GetServer(endpoints.First());
            var keys = new List<string>();

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                keys.Add(key.ToString());
            }

            return keys;
        }

        // ---------------- HASH ----------------

        public async Task<bool> HashSetAsync(string key, string field, string value)
        {
            return await _db.HashSetAsync(key, field, value);
        }

        public async Task<bool> HashSetObjectAsync<T>(string key, string field, T value)
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            return await _db.HashSetAsync(key, field, json);
        }

        public async Task<string?> HashGetAsync(string key, string field)
        {
            var value = await _db.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }


        public async Task<Dictionary<string, string>> HashGetAllAsync(string key)
        {
            var entries = await _db.HashGetAllAsync(key);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => e.Value.ToString()
            );
        }

        public async Task<bool> HashDeleteAsync(string key, string field)
        {
            return await _db.HashDeleteAsync(key, field);
        }

        public async Task<bool> HashExistsAsync(string key, string field)
        {
            return await _db.HashExistsAsync(key, field);
        }

        // ---------------- LIST ----------------

        public async Task<long> ListLeftPushAsync(string key, string value)
        {
            return await _db.ListLeftPushAsync(key, value);
        }

        public async Task<long> ListRightPushAsync(string key, string value)
        {
            return await _db.ListRightPushAsync(key, value);
        }

        public async Task<string?> ListLeftPopAsync(string key)
        {
            var value = await _db.ListLeftPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<string?> ListRightPopAsync(string key)
        {
            var value = await _db.ListRightPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }

        public async Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1)
        {
            var values = await _db.ListRangeAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToList();
        }

        public async Task<long> ListLengthAsync(string key)
        {
            return await _db.ListLengthAsync(key);
        }

        // ---------------- SET ----------------

        public async Task<bool> SetAddAsync(string key, string value)
        {
            return await _db.SetAddAsync(key, value);
        }

        public async Task<bool> SetRemoveAsync(string key, string value)
        {
            return await _db.SetRemoveAsync(key, value);
        }

        public async Task<bool> SetContainsAsync(string key, string value)
        {
            return await _db.SetContainsAsync(key, value);
        }

        public async Task<List<string>> SetMembersAsync(string key)
        {
            var values = await _db.SetMembersAsync(key);
            return values.Select(v => v.ToString()).ToList();
        }

        public async Task<long> SetLengthAsync(string key)
        {
            return await _db.SetLengthAsync(key);
        }

        // ---------------- SORTED SET ----------------

        public async Task<bool> SortedSetAddAsync(string key, string member, double score)
        {
            return await _db.SortedSetAddAsync(key, member, score);
        }

        public async Task<bool> SortedSetRemoveAsync(string key, string member)
        {
            return await _db.SortedSetRemoveAsync(key, member);
        }

        public async Task<List<string>> SortedSetRangeByRankAsync(string key, long start = 0, long stop = -1, bool descending = false)
        {
            var order = descending ? Order.Descending : Order.Ascending;
            var values = await _db.SortedSetRangeByRankAsync(key, start, stop, order);
            return values.Select(v => v.ToString()).ToList();
        }

        public async Task<double?> SortedSetScoreAsync(string key, string member)
        {
            return await _db.SortedSetScoreAsync(key, member);
        }

        public async Task<long> SortedSetLengthAsync(string key)
        {
            return await _db.SortedSetLengthAsync(key);
        }

        // ---------------- INCREMENT / DECREMENT ----------------

        public async Task<long> IncrementAsync(string key, long value = 1)
        {
            return await _db.StringIncrementAsync(key, value);
        }

        public async Task<long> DecrementAsync(string key, long value = 1)
        {
            return await _db.StringDecrementAsync(key, value);
        }

        // ---------------- PUB/SUB ----------------

        public async Task PublishAsync(string channel, string message)
        {
            var subscriber = _connectionMultiplexer.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal(channel), message);
        }

        public async Task SubscribeAsync(string channel, Action<RedisChannel, RedisValue> handler)
        {
            var subscriber = _connectionMultiplexer.GetSubscriber();
            await subscriber.SubscribeAsync(RedisChannel.Literal(channel), handler);
        }

        // ---------------- TRANSACTION EXAMPLE ----------------

        public async Task<bool> SetMultipleAsync(Dictionary<string, string> keyValues)
        {
            var transaction = _db.CreateTransaction();

            foreach (var kv in keyValues)
            {
                _ = transaction.StringSetAsync(kv.Key, kv.Value);
            }

            return await transaction.ExecuteAsync();
        }
    }
}