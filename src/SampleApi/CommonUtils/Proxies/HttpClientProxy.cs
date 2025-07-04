using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SampleApi.CommonUtils.Models.Dtos.Request;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SampleApi.CommonUtils.Proxies
{
    public class HttpClientProxy
    {
        private readonly HttpClient _client;
        private readonly ILogger<HttpClientProxy> _logger;

        public HttpClientProxy(ILogger<HttpClientProxy> logger)
        {
            _logger = logger;
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(600) };
            _client.DefaultRequestHeaders.Add("x-ally-trace-id", RequestHeaderContext.CurrentTraceId.Value);
            _client.DefaultRequestHeaders.Add("x-ally-token-key", RequestHeaderContext.TokenKey.Value);
        }

        public async Task<TResponse?> SendGetRequest<TResponse>(string url, string? token = null)
        {
            SetupToken(token);
            _logger.LogInformation($"SendGetRequest Request URL: {url}");

            var response = await _client.GetAsync(url);

            return await DeserializeResponseAsync<TResponse>(response);
        }

        public async Task<TResponse?> SendGetRequest<TRequest, TResponse>(string url, TRequest request, string? token = null)
        {
            SetupToken(token);
            url = AppendQueryParameters(url, request);
            _logger.LogInformation($"SendGetRequest Request URL: {url}");

            var response = await _client.GetAsync(url);

            return await DeserializeResponseAsync<TResponse>(response);
        }

        public async Task<TResponse?> SendPostRequest<TRequest, TResponse>(
           string url,
           TRequest requestBody,
           string? token = null,
           Dictionary<string, string>? additionalHeaders = null)
        {
            var response = requestBody is IFormFile
                ? await SendPostRequestWithFormFile(url, requestBody)
                : await SendPostRequest(url, requestBody, token, additionalHeaders);

            return await DeserializeResponseAsync<TResponse>(response);
        }

        public async Task<HttpResponseMessage> SendPostRequest<TRequest>(
            string url,
            TRequest requestBody,
            string? token = null,
            Dictionary<string, string>? additionalHeaders = null)
        {
            var request = this.CreateHttpRequestMessage(url, requestBody, token, additionalHeaders);
            var response = await _client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Url: {url}, SendPostRequest Response Payload: {responseString}");
            return response;
        }

        public async Task<HttpResponseMessage> SendPostRequestWithFormFile<TRequest>(string url, TRequest requestBody)
        {
            using var formData = new MultipartFormDataContent();
            formData.Headers.Add("x-trace-id", RequestHeaderContext.CurrentTraceId.Value);
            formData.Headers.Add("x-token-key", RequestHeaderContext.TokenKey.Value);

            if (requestBody is IFormFile file)
            {
                formData.Add(new StreamContent(file.OpenReadStream()), "fileToUpload", file.FileName);
            }

            var response = await _client.PostAsync(url, formData);
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Url: {url}, SendPostRequest Response Payload: {responseString}");
            return response;
        }

        public async Task<TResponse?> SendPutRequest<TRequest, TResponse>(string url, TRequest requestBody, string? token = null)
        {
            var json = JsonConvert.SerializeObject(requestBody);
            _logger.LogInformation($"Url: {url}, SendPutRequest Request Payload: {json}");
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            request.Headers.Add("x-ally-trace-id", RequestHeaderContext.CurrentTraceId.Value);
            request.Headers.Add("x-ally-token-key", RequestHeaderContext.TokenKey.Value);
            SetupToken(token);

            var response = await _client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Url: {url}, SendPutRequest Response Payload: {responseString}");

            return await DeserializeResponseAsync<TResponse>(response);
        }

        public async Task<TResponse?> SendDeleteRequest<TResponse>(string url, string? token = null)
        {
            SetupToken(token);
            _logger.LogInformation($"SendDeleteRequest Request URL: {url}");

            var response = await _client.DeleteAsync(url);

            return await DeserializeResponseAsync<TResponse>(response);
        }

        public async Task<TResponse?> SendDeleteRequest<TRequest, TResponse>(string url, TRequest request, string? token = null)
        {
            SetupToken(token);
            url = AppendQueryParameters(url, request);
            _logger.LogInformation($"SendDeleteRequest Request URL: {url}");

            var response = await _client.DeleteAsync(url);

            return await DeserializeResponseAsync<TResponse>(response);
        }

        private HttpRequestMessage CreateHttpRequestMessage<TRequest>(
            string url,
            TRequest requestBody,
            string? token = null,
            Dictionary<string, string>? additionalHeaders = null)
        {
            var json = JsonConvert.SerializeObject(requestBody);
            _logger.LogInformation($"Url: {url}, SendPostRequest Request Payload: {json}");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/json") } },
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("x-trace-id", RequestHeaderContext.CurrentTraceId.Value);
            request.Headers.Add("x-token-key", RequestHeaderContext.TokenKey.Value);

            SetupToken(token);

            if (additionalHeaders != null)
            {
                foreach (var header in additionalHeaders)
                {
                    if (!request.Headers.Contains(header.Key))
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            return request;
        }

        private string AppendQueryParameters<TRequest>(string url, TRequest request)
        {
            var queryParams = request.GetType().GetProperties()
                .Where(prop => prop.GetValue(request) != null)
                .ToDictionary(
                    prop => prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name,
                    prop => prop.GetValue(request)?.ToString() ?? string.Empty);

            var uriBuilder = new UriBuilder(url);
            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (var param in queryParams)
            {
                query[param.Key] = param.Value;
            }

            uriBuilder.Query = query.ToString();
            return uriBuilder.ToString();
        }

        private void SetupToken(string? token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        private async Task<TResponse?> DeserializeResponseAsync<TResponse>(HttpResponseMessage response)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Raw Response JSON: {responseString}");
            bool flag = typeof(TResponse).GetProperties().Any((PropertyInfo prop) => Attribute.IsDefined(prop, typeof(JsonPropertyAttribute)));
            _logger.LogInformation("Using " + (flag ? "Newtonsoft.Json" : "System.Text.Json") + " for deserialization.");
            if (!flag)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                return System.Text.Json.JsonSerializer.Deserialize<TResponse>(responseString, options);
            }
            else
            {
                var settings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy(),
                    },
                };

                return JsonConvert.DeserializeObject<TResponse>(responseString, settings);
            }
        }
    }
}
