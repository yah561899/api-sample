using Microsoft.Extensions.Diagnostics.HealthChecks;
using SampleApi.CommonUtils.Extensions;
using SampleApi.CommonUtils.Models.Enums;
using SampleApi.CommonUtils.Tools.Env;
using StackExchange.Redis;

namespace SampleApi.Utils.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddServiceCollection(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddHttpClient();
            services.AddLogging();

            services.NuGetCollection();
            services.HostServiceCollection();
            services.SingletonCollection();
            services.ScopeCollection();
            services.TransientCollection();

            //services.AddAutoMapper(typeof(Program));
        }

        public static IConfigurationBuilder ConfigurAllyConfiguration(this IConfigurationBuilder config)
        {
            config.SetBasePath(AppContext.BaseDirectory);

            return config;
        }

        private static void NuGetCollection(this IServiceCollection services)
        {
            services.AddCachingHelper();
            services.AddInnerHttpClient();
            services.AddCorsPolicy();
            services.AddHealthCheck();
            //services.AddMqClient(id =>
            //{
            //    RequestHeaderContext.CurrentTraceId.Value = id;
            //});

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = $"{EnvironmentVariableReader<EnumCommonEnvironmentVariable>.Get(EnumCommonEnvironmentVariable.RedisHost)}:{EnvironmentVariableReader<EnumCommonEnvironmentVariable>.Get(EnumCommonEnvironmentVariable.RedisPort)}";
            });
        }

        private static void HostServiceCollection(this IServiceCollection services)
        {
        }

        private static void SingletonCollection(this IServiceCollection services)
        {
        }

        private static void ScopeCollection(this IServiceCollection services)
        {
        }

        private static void TransientCollection(this IServiceCollection services)
        {
        }

        private static void AddHealthCheck(this IServiceCollection services)
        {
            services
                .AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        }

        private static void AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("OpenCorsPolicy", builder =>
                {
                    builder
                        .WithOrigins(
                            "http://localhost:8000",
                            "http://127.0.0.1:8000")
                        .AllowCredentials()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
        }
    }
}
