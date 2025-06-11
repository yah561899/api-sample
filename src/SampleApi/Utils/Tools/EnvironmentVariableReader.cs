using SampleApi.Utils.Attributes;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace SampleApi.Utils.Tools
{
    public static class EnvironmentVariableReader<TEnum>
        where TEnum : Enum
    {
        private static readonly ConcurrentDictionary<TEnum, (string Value, DateTime LastUpdateTime)> EnvironmentVariablesCache =
            new ConcurrentDictionary<TEnum, (string, DateTime)>();

        private static readonly int CacheDurationInSeconds = 60;

        static EnvironmentVariableReader()
        {
        }

        /// <summary>
        /// Get environmental variable
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        public static string? Get(TEnum variable)
        {
            if (IsValueCached(variable, out var cachedValue))
            {
                return cachedValue;
            }

            string variableName = GetVariableName(variable);

            bool isOptional = typeof(TEnum)
                .GetField(variable.ToString())?
                .GetCustomAttributes(typeof(OptionalEnvironmentAttribute), false)
                .Any() == true;

            var environmentVariable = GetEnvironmentVariableValue(variableName, isOptional);

            UpdateEnvironmentVariableCache(variable, environmentVariable);

            return environmentVariable;
        }

        private static string GetVariableName(TEnum variable)
        {
            string variableName = variable.ToString();

            var displayAttribute = typeof(TEnum)
                .GetField(variableName)?
                .GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault() as DisplayAttribute;

            return displayAttribute?.Name ?? variableName;
        }

        private static string? GetEnvironmentVariableValue(string variableName, bool isOptional)
        {
            string? environmentVariable = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process);

            if (environmentVariable == null)
            {
                // environmental value does not exist but it is required.
                if (!isOptional)
                {
                    throw new Exception($"environmental value '{variableName}' have not been found.");
                }

                environmentVariable = null;
            }

            return environmentVariable;
        }

        private static bool IsValueCached(TEnum variable, out string? cachedValue)
        {
            if (EnvironmentVariablesCache.TryGetValue(variable, out var cachedData))
            {
                if ((DateTime.Now - cachedData.LastUpdateTime).TotalSeconds < CacheDurationInSeconds)
                {
                    cachedValue = cachedData.Value;
                    return true;
                }
            }

            cachedValue = null;
            return false;
        }

        private static void UpdateEnvironmentVariableCache(TEnum variable, string? environmentVariable)
        {
            if (environmentVariable == null)
            {
                return;
            }

            EnvironmentVariablesCache.AddOrUpdate(variable, (environmentVariable, DateTime.Now), (key, oldValue) => (environmentVariable, DateTime.Now));
        }
    }
}
