using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using SampleApi.CommonUtils.Attributes;
using SampleApi.CommonUtils.Models.Dtos.Redis;

namespace SampleApi.CommonUtils.Tools.Cache
{
    public class CachingHelper : ICachingHelper
    {
        private readonly ILogger<CachingHelper> logger;
        private readonly IRedisClient redisClient;
        private JsonSerializerSettings jsonSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingHelper"/> class.
        /// </summary>
        /// <param name="cache"></param>
        public CachingHelper(
            ILogger<CachingHelper> logger,
            IRedisClient cache)
        {
            this.logger = logger;
            redisClient = cache;
            jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy(),
                },
            };
        }

        public IDatabase GetDatabase()
        {
            return redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
        }

        /// <summary>
        /// CacheDataAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task CacheDataAsync<T>(string key, T data, TimeSpan? expiry = null, CommandFlags commandFlags = CommandFlags.None)
        {
            try
            {
                var redisKey = new RedisKey(key);
                var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
                var serializedData = JsonConvert.SerializeObject(data, jsonSettings);

                await cache.StringSetAsync(redisKey, serializedData, expiry, false, When.Always, commandFlags);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// GetDataAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> GetDataAsync<T>(string key, CommandFlags commandFlags = CommandFlags.None)
        {
            try
            {
                var redisKey = new RedisKey(key);
                var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
                var serializedData = await cache.StringGetAsync(redisKey, commandFlags);
                logger.LogInformation(serializedData);
                if (string.IsNullOrEmpty(serializedData))
                {
                    return default;
                }

                return JsonConvert.DeserializeObject<T>(serializedData, jsonSettings);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return default;
            }
        }

        public async Task<T> GetDataAsync<T, TParam>(CacheGetOptions<T, TParam> options)
        where T : class?
        {
            try
            {
                var key = options.Keys.First();
                var redisKey = new RedisKey(key);
                var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
                var serializedData = await cache.StringGetAsync(redisKey, options.CommandFlags);
                logger.LogInformation(serializedData);

                if (!string.IsNullOrEmpty(serializedData) && !options.Force)
                {
                    return JsonConvert.DeserializeObject<T>(serializedData, jsonSettings);
                }

                var data = await options.GetFunc(options.Param);
                if (data != null)
                {
                    await CacheDataAsync(key, data, options.Expiry, options.CommandFlags);
                }

                return data;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public async Task<List<T>> GetDataListAsync<T, TParam>(CacheGetOptions<T, TParam> options)
        where T : class?
        {
            try
            {
                var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
                var resultList = new List<T>();
                var missingKeys = new List<string>();

                foreach (var key in options.Keys)
                {
                    var newKey = key;
                    if (!string.IsNullOrEmpty(options.Prefix))
                    {
                        newKey = $"{options.Prefix}-{newKey}";
                    }

                    var redisKey = new RedisKey(newKey);
                    var serializedData = await cache.StringGetAsync(redisKey, options.CommandFlags);
                    logger.LogInformation($"Key: {newKey}, Data: {serializedData}");

                    if (!string.IsNullOrEmpty(serializedData) && !options.Force)
                    {
                        var data = JsonConvert.DeserializeObject<T>(serializedData, jsonSettings);
                        if (data != null)
                        {
                            resultList.Add(data);
                        }
                    }
                    else
                    {
                        missingKeys.Add(key);
                    }
                }

                if (missingKeys.Count > 0)
                {
                    var missingData = await options.GetListFunc(options.Param);
                    if (missingData != null && missingData.Count > 0)
                    {
                        foreach (var item in missingData)
                        {
                            var cacheKey = item.GetType().GetProperties()
                                .FirstOrDefault(prop => Attribute.IsDefined(prop, typeof(CacheKeyAttribute)))?
                                .GetValue(item)?.ToString();

                            if (!string.IsNullOrEmpty(options.Prefix))
                            {
                                cacheKey = $"{options.Prefix}-{cacheKey}";
                            }

                            if (!string.IsNullOrEmpty(cacheKey))
                            {
                                await CacheDataAsync(cacheKey, item, options.Expiry, options.CommandFlags);
                            }
                        }

                        resultList.AddRange(missingData);
                    }
                }

                return resultList;
            }
            catch (RedisException redisEx)
            {
                logger.LogError(redisEx, "Redis exception occurred while getting data.");
                return default;
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "Json deserialization error.");
                return default;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return default;
            }
        }

        /// <summary>
        /// RemoveDataAsync
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task RemoveDataAsync(string key, CommandFlags commandFlags = CommandFlags.None)
        {
            try
            {
                var redisKey = new RedisKey(key);
                var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
                var serializedData = await cache.KeyDeleteAsync(redisKey, commandFlags);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public async Task TakeLockAsync(string lockKey, TimeSpan lockTime)
        {
            var expiry = lockTime;

            var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
            bool acquired = await cache.LockTakeAsync(lockKey, Environment.MachineName, expiry);
            if (acquired)
            {
                logger.LogInformation($"Lock acquired for {lockKey}");
            }
            else
            {
                logger.LogError($"Could not acquire lock for {lockKey}");
                throw new Exception($"Could not acquire lock for {lockKey}");
            }
        }

        public async Task ReleaseLockAsync(string lockKey)
        {
            var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
            bool released = await cache.LockReleaseAsync(lockKey, Environment.MachineName);
            if (released)
            {
                logger.LogInformation($"Lock released for {lockKey}");
            }
            else
            {
                logger.LogError($"Failed to release lock for {lockKey}");
            }
        }

        public async Task CheckExistLocksAsync(List<string> lockKeys)
        {
            var cache = redisClient.ConnectionPoolManager.GetConnection().GetDatabase();
            bool allLocksReleased = false;
            while (!allLocksReleased)
            {
                allLocksReleased = true;
                foreach (var lockKey in lockKeys)
                {
                    bool exists = await cache.KeyExistsAsync(lockKey);
                    if (exists)
                    {
                        allLocksReleased = false;
                        break;
                    }
                }

                if (!allLocksReleased)
                {
                    await Task.Delay(500);
                }
            }

            logger.LogInformation("All locks have been released");
        }
    }
}
