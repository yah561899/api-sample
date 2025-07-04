using SampleApi.CommonUtils.Tools.Cache;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.Core;
using SampleApi.CommonUtils.Tools.Env;
using SampleApi.CommonUtils.Models.Enums;
using SampleApi.CommonUtils.Proxies;

namespace SampleApi.CommonUtils.Extensions
{
    public static class CommonServiceCollectionExtensions
    {
        public static bool CachingHelperIsReady = false;

        public static IServiceCollection AddCachingHelper(this IServiceCollection services)
        {
            if (!CachingHelperIsReady)
            {
                RedisConfiguration redisConfiguration = GetRedisConfiguration();

                ISerializer serializer = new RedisSerializer();
                services.AddSingleton(serializer);

                var connectionPoolManager = new RedisConnectionPoolManager(redisConfiguration);
                var redisClient = new RedisClient(connectionPoolManager, serializer, redisConfiguration);

                services.AddSingleton<IRedisClient>(redisClient);
                services.AddSingleton<ICachingHelper, CachingHelper>();
                CachingHelperIsReady = true;
            }

            return services;
        }

        public static IServiceCollection AddInnerHttpClient(this IServiceCollection services)
        {
            services.AddTransient<HttpClientProxy>();

            return services;
        }

        private static RedisConfiguration GetRedisConfiguration()
        {
            var res = new RedisConfiguration
            {
                Hosts = new RedisHost[]
                {
                    new RedisHost()
                    {
                        Host = EnvironmentVariableReader<EnumCommonEnvironmentVariable>.Get(EnumCommonEnvironmentVariable.RedisHost),
                        Port = Convert.ToInt32(EnvironmentVariableReader<EnumCommonEnvironmentVariable>.Get(EnumCommonEnvironmentVariable.RedisPort)),
                    },
                },
            };

            return res;
        }
    }
}
