namespace SampleApi.Utils.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class OptionalEnvironmentAttribute : Attribute
    {
    }
}
