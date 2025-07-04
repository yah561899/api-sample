namespace SampleApi.CommonUtils.Models.Dtos.Request
{
    public static class RequestHeaderContext
    {
        public static AsyncLocal<string> CurrentTraceId { get; } = new AsyncLocal<string>();


        public static AsyncLocal<string> TokenKey { get; } = new AsyncLocal<string>();


        //public static AsyncLocal<DtoUserInfo> UserInfo { get; } = new AsyncLocal<DtoUserInfo>();
        public static AsyncLocal<string> UserId { get; } = new AsyncLocal<string>();
    }
}
