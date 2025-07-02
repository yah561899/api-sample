namespace SampleApi.Models.Dtos
{
    public class WorkflowExecutionInfo
    {
        public Guid Id { get; set; }

        public required string StepName { get; set; }

        public string? ApiUrl { get; set; }

        public bool? IsSuccess { get; set; }

        public Dictionary<string, object> RequestPayload { get; set; } = [];

        public Dictionary<string, object> ResponsePayload { get; set; } = [];

        public long? StartTime { get; set; }

        public long? EndTime { get; set; }

        public long? Duration { get; set; }
    }
}
