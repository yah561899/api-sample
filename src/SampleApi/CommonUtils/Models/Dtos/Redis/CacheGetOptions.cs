using StackExchange.Redis;

namespace SampleApi.CommonUtils.Models.Dtos.Redis
{
    /// <summary>
    /// 用於包裝 Cache 查詢參數，簡化呼叫參數量，並保留向下相容。
    /// </summary>
    public class CacheGetOptions<T, TParam>
        where T : class?
    {
        public List<string> Keys { get; set; } = new();

        public Func<TParam?, Task<T>>? GetFunc { get; set; }

        public Func<TParam?, Task<List<T>>>? GetListFunc { get; set; }

        public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(5);

        public bool Force { get; set; } = false;

        public TParam? Param { get; set; } = default;

        public CommandFlags CommandFlags { get; set; } = CommandFlags.None;

        public string? Prefix { get; set; }
    }
}
