using System.Text.RegularExpressions;

namespace FIGCommon.Providers
{
    public class ConfigurationProviderWithVars : ConfigurationProvider
    {
        private const string GlobalVarsPrefix = "Global:Vars:";
        private static readonly Regex VariablePattern = new(
            @"\{\{(?<key>[^{}]+)\}\}",
            RegexOptions.Compiled);

        private readonly IConfiguration _baseConfiguration;

        public ConfigurationProviderWithVars(IConfiguration baseConfiguration)
        {
            _baseConfiguration = baseConfiguration;
        }

        public override void Load()
        {
            // Take one snapshot so resolution is deterministic while this
            // provider is being populated.
            var sourceValues = _baseConfiguration.AsEnumerable().ToArray();
            var variables = BuildVariableMap(sourceValues);

            foreach (var kvp in sourceValues)
            {
                Data[kvp.Key] = kvp.Value == null
                    ? null
                    : ResolveValue(kvp.Value, variables, new List<string>());
            }
        }

        private static Dictionary<string, string> BuildVariableMap(
            IEnumerable<KeyValuePair<string, string?>> sourceValues)
        {
            var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Include regular configuration keys, including top-level keys
            // loaded from global_vars.json.
            foreach (var kvp in sourceValues)
            {
                if (kvp.Value != null &&
                    !kvp.Key.StartsWith(GlobalVarsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    variables[kvp.Key] = kvp.Value;
                }
            }

            // Service-specific Global:Vars values take precedence and are
            // exposed without their section prefix (for example Service_Id).
            foreach (var kvp in sourceValues)
            {
                if (kvp.Value != null &&
                    kvp.Key.StartsWith(GlobalVarsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string key = kvp.Key[GlobalVarsPrefix.Length..];
                    variables[key] = kvp.Value;
                }
            }

            return variables;
        }

        private static string ResolveValue(
            string value,
            IReadOnlyDictionary<string, string> variables,
            List<string> resolutionPath)
        {
            return VariablePattern.Replace(value, match =>
            {
                string key = match.Groups["key"].Value;

                // Preserve unknown placeholders so startup validation can
                // report the unresolved configuration value clearly.
                if (!variables.TryGetValue(key, out string? replacement))
                    return match.Value;

                if (resolutionPath.Contains(key, StringComparer.OrdinalIgnoreCase))
                {
                    string cycle = string.Join(" -> ", resolutionPath.Append(key));
                    throw new InvalidOperationException(
                        $"Circular configuration variable reference detected: {cycle}");
                }

                resolutionPath.Add(key);
                try
                {
                    // Resolve placeholders inside the replacement itself. This
                    // supports Controller_Connection -> Controller_Port.
                    return ResolveValue(replacement, variables, resolutionPath);
                }
                finally
                {
                    resolutionPath.RemoveAt(resolutionPath.Count - 1);
                }
            });
        }
    }
}
