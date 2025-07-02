using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SampleApi.CommonUtils.Extensions
{
    public static class EnumExtensions
    {
        public static T ToEnumFromDisplayName<T>(this string displayName) where T : Enum
        {
            Type typeFromHandle = typeof(T);
            string[] names = Enum.GetNames(typeFromHandle);
            foreach (string text in names)
            {
                DisplayAttribute customAttribute = typeFromHandle.GetMember(text).First().GetCustomAttribute<DisplayAttribute>();
                if (customAttribute != null && customAttribute.Name.Equals(displayName, StringComparison.OrdinalIgnoreCase))
                {
                    return (T)Enum.Parse(typeFromHandle, text);
                }
            }

            throw new ArgumentException($"No enum value with display name '{displayName}' in enum '{typeFromHandle.Name}'");
        }
    }
}
