using SampleApi.CommonUtils.Models.Dtos.Redis;
using StackExchange.Redis;

namespace SampleApi.CommonUtils.Tools.Cache
{
    public interface ICachingHelper
    {
        /// <summary>
        /// GetDatabase
        /// </summary>
        /// <returns></returns>
        IDatabase GetDatabase();

        Task CacheDataAsync<T>(string key, T data, TimeSpan? expiry = null, CommandFlags commandFlags = CommandFlags.None);

        Task<T> GetDataAsync<T>(string key, CommandFlags commandFlags = CommandFlags.None);

        Task<T> GetDataAsync<T, TParam>(CacheGetOptions<T, TParam> options)
        where T : class?;

        Task<List<T>> GetDataListAsync<T, TParam>(CacheGetOptions<T, TParam> options)
        where T : class?;

        Task RemoveDataAsync(string key, CommandFlags commandFlags = CommandFlags.None);

        Task TakeLockAsync(string lockKey, TimeSpan lockTime);

        Task ReleaseLockAsync(string lockKey);

        Task CheckExistLocksAsync(List<string> lockKeys);
    }
}
