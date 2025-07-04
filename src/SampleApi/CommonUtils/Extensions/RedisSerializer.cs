using StackExchange.Redis.Extensions.Core;
using System.Text;
using System.Text.Json;

namespace SampleApi.CommonUtils.Extensions
{
    public class RedisSerializer : ISerializer
    {
        public byte[] Serialize<T>(T item)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(item));
        }

        public T Deserialize<T>(byte[] serializedObject)
        {
            return JsonSerializer.Deserialize<T>(serializedObject.ToString());
        }
    }
}
