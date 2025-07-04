namespace SampleApi.Utils.Extensions
{
    public static class HostBuilderExtension
    {
        public static IWebHostBuilder ConfigureWebHost(this IWebHostBuilder builder)
        {
            builder.UseKestrel(options =>
            {
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(600);
                options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(600);
            });

            return builder;
        }
    }
}
