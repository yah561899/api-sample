using SampleApi.CommonUtils.Models.Dtos.Request;
using SampleApi.CommonUtils.Tools.Cache;

namespace SampleApi.CommonUtils.Middlewares
{
    public class HeaderReaderMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<HeaderReaderMiddleware> _logger;

        private readonly ICachingHelper _cachingHelper;

        public HeaderReaderMiddleware(RequestDelegate next, ILogger<HeaderReaderMiddleware> logger, ICachingHelper cachingHelper)
        {
            _next = next;
            _logger = logger;
            _cachingHelper = cachingHelper;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string? text = GetHeaderValueInOrder(context.Request, new List<string> { "x-trace-id" });
            string? headerValueInOrder = GetHeaderValueInOrder(context.Request, new List<string> { "x-token-key" });
            if (string.IsNullOrEmpty(text))
            {
                text = Guid.NewGuid().ToString();
                context.Request.Headers.Append("x-trace-id", text);
            }

            RequestHeaderContext.CurrentTraceId.Value = text;
            if (!string.IsNullOrEmpty(headerValueInOrder))
            {
                context.Request.Headers.Append("x-token-key", headerValueInOrder);
                RequestHeaderContext.TokenKey.Value = headerValueInOrder;
                if (!string.IsNullOrEmpty(RequestHeaderContext.TokenKey.Value))
                {
                    //DtoUserToken dtoUserToken = await _cachingHelper.GetDataAsync<DtoUserToken>(RequestHeaderContext.AllyTokenKey.Value);
                    //if (dtoUserToken == null)
                    //{
                    //    context.Response.StatusCode = 401;
                    //    return;
                    //}

                    //RequestHeaderContext.UserInfo.Value = dtoUserToken.Token.User;
                }
                else
                {
                    _logger.LogWarning("No Auth User Info");
                }
            }

            await _next(context);
        }

        public string? GetHeaderValueInOrder(HttpRequest request, List<string> headerKeys)
        {
            foreach (string headerKey in headerKeys)
            {
                if (request.Headers.TryGetValue(headerKey, out var value) && !string.IsNullOrEmpty(value))
                {
                    _logger.LogDebug(value.ToString());
                    return value;
                }
            }

            return null;
        }
    }
}
