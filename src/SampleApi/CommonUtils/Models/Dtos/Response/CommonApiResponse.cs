namespace SampleApi.CommonUtils.Models.Dtos.Response
{
    public class CommonApiResponse<TData>
    {
        public string Code { get; set; }

        public string TraceId { get; set; }

        public TData Data { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
