namespace SampleApi.CommonUtils.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OptionalEnvironmentAttribute : Attribute
    {
    }
}
