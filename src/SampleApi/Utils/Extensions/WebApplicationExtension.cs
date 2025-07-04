using SampleApi.CommonUtils.Middlewares;

namespace SampleApi.Utils.Extensions
{
    public static class WebApplicationExtension
    {
        public static void AddWebApplication(this WebApplication app, string serviceBaseUrl)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseCors("OpenCorsPolicy");

            app.UseMiddleware<HeaderReaderMiddleware>();
            //app.UseMiddleware<LoggingMiddleware>();
            //app.UseMiddleware<ApiResponseMiddleware>();

            app.Urls.Clear();
            app.Urls.Add(serviceBaseUrl);

            app.Run();
        }
    }
}
