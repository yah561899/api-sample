using SampleApi.CommonUtils.Models.Dtos.Request;
using SampleApi.CommonUtils.Tools.Cache;
using SampleApi.Models.Dtos;
using SampleApi.Models.Enums;
using StackExchange.Redis;
using static SampleApi.Models.Dtos.WorkflowModel;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using SampleApi.CommonUtils.Models.Dtos.Response;

namespace SampleApi.Executor
{
    public class WorkflowExecutor
    {
        private readonly ILogger<WorkflowExecutor> _logger;
        private readonly ICachingHelper _cachingHelper;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public WorkflowExecutor(
            ILogger<WorkflowExecutor> logger,
            ICachingHelper cachingHelper,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _cachingHelper = cachingHelper;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<List<WorkflowExecutionInfo>> InitialWorkflow(
            string workflowId,
            Dictionary<string, object> payload)
        {
            var workflowPayload = await _cachingHelper.GetDataAsync<WorkflowModel.Workflow>(workflowId);
            var workflowExecutionInfos = new ConcurrentDictionary<Guid, WorkflowExecutionInfo>();
            await ProcessWorkflow(workflowExecutionInfos, workflowPayload.Flow, payload);
            var res = new List<WorkflowExecutionInfo>(workflowExecutionInfos.ToArray().Select(e => e.Value).OrderBy(e => e.StartTime));
            return res;
        }

        public async Task<Dictionary<string, object>?> ProcessWorkflow(
            ConcurrentDictionary<Guid, WorkflowExecutionInfo> workflowExecutionInfos,
            IEnumerable<WorkflowNode> flow,
            Dictionary<string, object> payload,
            bool? isSuccess = null)
        {
            foreach (var node in flow)
            {
                if (isSuccess.HasValue && !isSuccess.Value)
                {
                    _logger.LogError($"step id: {node.Id}, module: {node.Module}, api name: {node.Mapper?.Name ?? "null"}, step conditions not met");
                    InsertWorkflowExecutionInfo(workflowExecutionInfos, node, true, payload, false);
                    return null;
                }

                Dictionary<string, object>? responsePayload = null;

                InsertWorkflowExecutionInfo(workflowExecutionInfos, node, true, payload, null);
                if (node.Filter != null && !EvaluateFilter(node.Filter, payload))
                {
                    _logger.LogError($"step id: {node.Id}, module: {node.Module}, api name: {node.Mapper?.Name ?? "null"}, step conditions not met");
                    InsertWorkflowExecutionInfo(workflowExecutionInfos, node, false, responsePayload ?? [], false);
                    return null;
                }

                switch (node.EnumModule)
                {
                    case EnumModule.OrbitApi:
                        responsePayload = await CallOtherServiceApi(workflowExecutionInfos, node, payload);
                        break;
                    case EnumModule.SetVariables:
                        responsePayload = SetVariables(node.Mapper.Variables, payload, isAppendVariables: false);
                        break;
                    case EnumModule.Router:
                        await ProcessRouter(workflowExecutionInfos, node.Routes, payload);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported module type: {node.Module}");
                }

                InsertWorkflowExecutionInfo(workflowExecutionInfos, node, false, responsePayload ?? [], true);

                payload = responsePayload ?? payload;
            }

            return payload;
        }

        private void InsertWorkflowExecutionInfo(
            ConcurrentDictionary<Guid, WorkflowExecutionInfo> _workflowExecutionInfos,
            WorkflowNode node,
            bool isRequest,
            Dictionary<string, object> payload,
            bool? isSuccess = null)
        {
            var nodePayload = new Dictionary<string, object>(payload);

            _workflowExecutionInfos.AddOrUpdate(
                node.Id,
                key => new WorkflowExecutionInfo
                {
                    Id = key,
                    StepName = node.Module,
                    ApiUrl = node.EnumModule == EnumModule.OrbitApi ? node.Mapper.Name : null,
                    IsSuccess = isSuccess,
                    RequestPayload = isRequest ? nodePayload : [],
                    ResponsePayload = !isRequest ? nodePayload : [],
                    StartTime = isRequest ? new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds() : null,
                },
                (key, existingInfo) =>
                {
                    existingInfo.IsSuccess = isSuccess;
                    if (isRequest) existingInfo.RequestPayload = nodePayload;
                    else existingInfo.ResponsePayload = nodePayload;
                    existingInfo.StepName = node.Module;
                    existingInfo.ApiUrl = node.EnumModule == EnumModule.OrbitApi ? node.Mapper.Name : null;
                    existingInfo.StartTime = existingInfo.StartTime;
                    return existingInfo;
                });
        }

        private async Task ProcessRouter(
            ConcurrentDictionary<Guid, WorkflowExecutionInfo> workflowExecutionInfos,
            IEnumerable<WorkflowRoute> routes,
            Dictionary<string, object> payload)
        {
            Dictionary<string, object>? originRouteData = new(payload);

            var routeTasks = routes.Select(route => Task.Run(async () =>
            {
                var routePayload = new Dictionary<string, object>(originRouteData);
                foreach (var flowNode in route.Flow)
                {
                    if (flowNode.Filter != null && !EvaluateFilter(flowNode.Filter, routePayload))
                    {
                        await ProcessWorkflow(workflowExecutionInfos, new[] { flowNode }, routePayload, false);
                        break;
                    }

                    routePayload = await ProcessWorkflow(workflowExecutionInfos, new[] { flowNode }, routePayload);
                }
            })).ToList();

            // Add a timeout of 1 minute
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1));

            var completedTask = await Task.WhenAny(Task.WhenAll(routeTasks), timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Processing routes did not complete within the allocated time of 1 minute.");
            }

            // Await all tasks to ensure exceptions in individual tasks are observed
            await Task.WhenAll(routeTasks);
        }


        private bool EvaluateFilter(Filter filter, Dictionary<string, object> payload)
        {
            foreach (var conditionGroup in filter.Conditions)
            {
                bool groupResult = conditionGroup.All(condition =>
                {
                    var fieldValue = GetFieldValue(payload, condition.Field);
                    var eva = EvaluateCondition(fieldValue, condition);
                    return eva;
                });

                if (groupResult)
                    return true;
            }

            return false;
        }

        private object? GetFieldValue(Dictionary<string, object> payload, string field)
        {
            var caseInsensitivePayload = new Dictionary<string, object>(payload, StringComparer.OrdinalIgnoreCase);

            if (field.Contains('.'))
            {
                var parts = field.Split('.');
                var currentField = parts[0];

                if (caseInsensitivePayload.TryGetValue(currentField, out var value))
                {
                    if (value is Dictionary<string, object> nestedPayload)
                    {
                        // Recurse into nested dictionary
                        return GetFieldValue(nestedPayload, string.Join('.', parts.Skip(1)));
                    }
                    else if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
                    {
                        // Handle JSON objects if needed
                        var nestedJson = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                        return GetFieldValue(nestedJson, string.Join('.', parts.Skip(1)));
                    }
                    else
                    {
                        return null; // If it's not a nested dictionary or JSON, the search ends here
                    }
                }
                return null; // Field not found
            }
            else
            {
                return caseInsensitivePayload.ContainsKey(field) ? caseInsensitivePayload[field] : null;
            }
        }

        private bool EvaluateCondition(object fieldValue, WorkflowModel.Condition condition)
        {
            if (condition is null)
                return true;

            if (fieldValue is null)
                return false;

            return condition.EnumOperater switch
            {
                // Text operators
                EnumOperaterType.EqualTo => fieldValue.ToString() == condition.Value,
                EnumOperaterType.NotEqualTo => fieldValue.ToString() != condition.Value,
                EnumOperaterType.Contains => fieldValue.ToString()?.Contains(condition.Value, StringComparison.OrdinalIgnoreCase) == true,
                EnumOperaterType.DoesNotContain => fieldValue.ToString()?.Contains(condition.Value, StringComparison.OrdinalIgnoreCase) == false,
                EnumOperaterType.StartsWith => fieldValue.ToString()?.StartsWith(condition.Value, StringComparison.OrdinalIgnoreCase) == true,
                EnumOperaterType.EndsWith => fieldValue.ToString()?.EndsWith(condition.Value, StringComparison.OrdinalIgnoreCase) == true,
                EnumOperaterType.MatchesPattern => Regex.IsMatch(fieldValue.ToString() ?? string.Empty, condition.Value),

                // Array operators
                EnumOperaterType.ArrayLengthEqualTo => IsJsonArray(fieldValue, out var array) && array.Count() == int.Parse(condition.Value),
                EnumOperaterType.ArrayLengthNotEqualTo => IsJsonArray(fieldValue, out var array) && array.Count() != int.Parse(condition.Value),
                EnumOperaterType.ArrayLengthGreaterThan => IsJsonArray(fieldValue, out var array) && array.Count() > int.Parse(condition.Value),
                EnumOperaterType.ArrayLengthLessThan => IsJsonArray(fieldValue, out var array) && array.Count() < int.Parse(condition.Value),

                _ => throw new NotSupportedException($"Unsupported operator type: {condition.Operator}")
            };
        }

        private bool IsJsonArray(object fieldValue, out IEnumerable<JsonElement> array)
        {
            array = Enumerable.Empty<JsonElement>();

            if (fieldValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                array = jsonElement.EnumerateArray();
                return true;
            }

            return false;
        }


        private Dictionary<string, object> SetVariables(IEnumerable<Variable> variables, Dictionary<string, object> payload, bool isAppendVariables)
        {
            var res = new Dictionary<string, object>();

            if (isAppendVariables)
            {
                foreach (var variable in variables)
                {
                    SetNestedValue(payload, variable.Name, variable.Value);
                }
            }
            else
            {
                foreach (var variable in variables)
                {
                    var value = GetFieldValue(payload, variable.Value);
                    SetNestedValue(res, variable.Name, value ?? variable.Value);
                }
            }

            return res;
        }

        private void SetNestedValue(Dictionary<string, object> dictionary, string path, object value)
        {
            var keys = path.Split('.');
            var current = dictionary;

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];

                if (i == keys.Length - 1)
                {
                    // Last key, set the value
                    current[key] = value;
                }
                else
                {
                    // Intermediate keys, ensure the structure exists
                    if (!current.ContainsKey(key) || current[key] is not Dictionary<string, object>)
                    {
                        current[key] = new Dictionary<string, object>();
                    }

                    current = (Dictionary<string, object>)current[key];
                }
            }
        }

        private async Task<Dictionary<string, object>?> CallOtherServiceApi(
            ConcurrentDictionary<Guid, WorkflowExecutionInfo> workflowExecutionInfos,
            WorkflowNode node,
            Dictionary<string, object> payload)
        {
            if (node.Mapper == null) throw new Exception("Mapper is required for ORBIT_API");

            if (node.Mapper.AppendVariables != null && node.Mapper.AppendVariables.Count() > 0)
            {
                SetVariables(node.Mapper.AppendVariables, payload, isAppendVariables: true);
            }

            InsertWorkflowExecutionInfo(workflowExecutionInfos, node, true, payload, null);

            using var client = new HttpClient();
            var requestPayloadStr = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(new HttpMethod(node.Mapper.Method), node.Mapper.Url)
            {
                Content = payload != null ? new StringContent(requestPayloadStr, Encoding.UTF8, "application/json") : null,
            };

            request.Headers.Add("x-ally-trace-id", RequestHeaderContext.CurrentTraceId.Value);
            _logger.LogInformation($"Url: {node.Mapper.Url}, request payload: {requestPayloadStr}");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadFromJsonAsync<CommonApiResponse<Dictionary<string, object>>>();
            if (responseBody == null || responseBody.Data == null)
            {
                throw new ArgumentNullException($"Url: {node.Mapper.Url}, response error, payload: {JsonSerializer.Serialize(responseBody)}");
            }
            _logger.LogInformation($"Url: {node.Mapper.Url}, request payload: {JsonSerializer.Serialize(responseBody)}");

            PushMqToApiGateway(node.Mapper, responseBody.Data);

            return responseBody.Data;
        }

        private void PushMqToApiGateway(Mapper mapper, Dictionary<string, object> data)
        {
            if (data["stepResultData"] == null)
                throw new ArgumentNullException(data["stepResultData"].ToString());

            if (!bool.TryParse(data["stepResult"].ToString(), out bool stepResult))
                throw new ArgumentNullException(data["stepResult"].ToString());

            var traceId = Guid.Parse(RequestHeaderContext.CurrentTraceId.Value);
            var userId = RequestHeaderContext.UserId.Value;

            var mqData = new WorkflowMqData<object>()
            {
                StageResult = new WorkflowStageResult()
                {
                    TraceId = traceId,
                    UserId = userId,
                    Data = data["stepResultData"],
                    StepKey = mapper.Name,
                    StepResult = stepResult,
                }
            };

            var pushModel = MqFactory.GetStepResult(traceId, userId, mqData);

            using var scope = _serviceScopeFactory.CreateScope();
            var mqProxy = scope.ServiceProvider.GetRequiredService<MqProxy>();
            mqProxy.PushToOtherService(pushModel);
        }
    }
}
