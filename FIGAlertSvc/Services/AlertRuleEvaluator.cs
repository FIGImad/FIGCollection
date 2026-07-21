using FIGCommon.Models.Alert;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FIGAlertSvc.Services
{
    /// <summary>
    /// Evaluates AlertRule RuleJson conditions against a persisted AlertRS record.
    /// </summary>
    public static class AlertRuleEvaluator
    {
        // ------------------------------------------------------------------ //
        //  Public entry point
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns every rule whose RuleJson matches <paramref name="alert"/>,
        /// or an empty list if no rule matches.
        /// </summary>
        public static List<AlertRuleRS> Evaluate(AlertRS alert, IEnumerable<AlertRuleRS> rules)
        {
            var matched = new List<AlertRuleRS>();
            foreach (var rule in rules)
            {
                if (!rule.Enabled) continue;
                try
                {
                    using var doc = JsonDocument.Parse(rule.RuleJson);
                    if (EvaluateNode(doc.RootElement, alert))
                    {
                        matched.Add(rule);
                    }
                }
                catch (JsonException)
                {
                    // malformed JSON — skip silently; caller may log
                }
            }
            return matched;
        }

        // ------------------------------------------------------------------ //
        //  Tree traversal
        // ------------------------------------------------------------------ //

        private static bool EvaluateNode(JsonElement node, AlertRS alert)
        {
            string op = node.GetProperty("operator").GetString() ?? "";

            return op.ToUpperInvariant() switch
            {
                "AND" => EvaluateGroup(node, alert, all: true),
                "OR"  => EvaluateGroup(node, alert, all: false),
                "NOT" => EvaluateNot(node, alert),
                _     => EvaluateCondition(node, alert, op)
            };
        }

        private static bool EvaluateGroup(JsonElement node, AlertRS alert, bool all)
        {
            if (!node.TryGetProperty("conditions", out JsonElement conditions))
                return false;

            foreach (var child in conditions.EnumerateArray())
            {
                bool result = EvaluateNode(child, alert);
                if (all && !result) return false;   // AND: short-circuit on first false
                if (!all && result) return true;    // OR:  short-circuit on first true
            }

            return all; // AND with all passing = true; OR with none passing = false
        }

        private static bool EvaluateNot(JsonElement node, AlertRS alert)
        {
            if (node.TryGetProperty("conditions", out JsonElement conditions))
            {
                var children = conditions.EnumerateArray().ToList();
                if (children.Count > 0)
                    return !EvaluateNode(children[0], alert);
            }
            return false;
        }

        // ------------------------------------------------------------------ //
        //  Leaf condition evaluation
        // ------------------------------------------------------------------ //

        private static bool EvaluateCondition(JsonElement node, AlertRS alert, string op)
        {
            if (!node.TryGetProperty("field", out JsonElement fieldElem)) return false;

            string fieldName = fieldElem.GetString() ?? "";
            string? fieldValue = GetFieldValue(alert, fieldName);

            // Null-check operators do not need a value
            if (op == "IS_NULL")    return fieldValue == null;
            if (op == "IS_NOT_NULL") return fieldValue != null;

            node.TryGetProperty("value", out JsonElement valueElem);
            string ruleValue = valueElem.ValueKind == JsonValueKind.Undefined ? "" : (valueElem.GetString() ?? "");

            bool caseSensitive = true;
            if (node.TryGetProperty("caseSensitive", out JsonElement csElem) && csElem.ValueKind == JsonValueKind.False)
                caseSensitive = false;

            StringComparison sc = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            string fv = fieldValue ?? "";

            return op.ToUpperInvariant() switch
            {
                "EQUALS"           => string.Equals(fv, ruleValue, sc),
                "NOT_EQUALS"       => !string.Equals(fv, ruleValue, sc),
                "CONTAINS"         => fv.Contains(ruleValue, sc),
                "NOT_LIKE"         => !MatchesLike(fv, ruleValue, caseSensitive),
                "LIKE"             => MatchesLike(fv, ruleValue, caseSensitive),
                "STARTS_WITH"      => fv.StartsWith(ruleValue, sc),
                "ENDS_WITH"        => fv.EndsWith(ruleValue, sc),
                "REGEX"            => Regex.IsMatch(fv, ruleValue,
                                        caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase),
                "GREATER_THAN"     => CompareNumericOrString(fv, ruleValue) > 0,
                "GREATER_OR_EQUAL" => CompareNumericOrString(fv, ruleValue) >= 0,
                "LESS_THAN"        => CompareNumericOrString(fv, ruleValue) < 0,
                "LESS_OR_EQUAL"    => CompareNumericOrString(fv, ruleValue) <= 0,
                "IN"               => IsIn(fv, ruleValue, sc),
                "NOT_IN"           => !IsIn(fv, ruleValue, sc),
                _                  => false
            };
        }

        // ------------------------------------------------------------------ //
        //  Helpers
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Maps an Alert field name (case-insensitive) to its string value.
        /// </summary>
        private static string? GetFieldValue(AlertRS alert, string fieldName)
        {
            return fieldName.ToUpperInvariant() switch
            {
                "SERVICEID"      => alert.ServiceId,
                "SERVICENAME"    => alert.ServiceName,
                "SERVICEROLE"    => alert.ServiceRole.ToString(),
                "SERVICEADDRESS" => alert.ServiceAddress,
                "LOGNAME"        => alert.LogName,
                "SOURCE"         => alert.Source,
                "LEVEL"          => alert.Level,
                "MESSAGE"        => alert.Message,
                "EXCEPTIONTEXT"  => alert.ExceptionText,
                "RAWTIME"        => alert.RawTime.ToString(),
                "MSEC"           => alert.MSec.ToString(),
                _                => null
            };
        }

        /// <summary>
        /// Translates a SQL LIKE pattern (% and _) into a regex and tests it.
        /// </summary>
        private static bool MatchesLike(string value, string pattern, bool caseSensitive)
        {
            // Escape all regex meta-chars except our translated ones
            string escaped = Regex.Escape(pattern)
                .Replace("%", ".*")
                .Replace("_", ".");

            RegexOptions opts = RegexOptions.Singleline |
                (caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);

            return Regex.IsMatch(value, $"^{escaped}$", opts);
        }

        /// <summary>
        /// Compares two values numerically when both parse as numbers, otherwise as strings.
        /// </summary>
        private static int CompareNumericOrString(string a, string b)
        {
            if (double.TryParse(a, out double da) && double.TryParse(b, out double db))
                return da.CompareTo(db);
            return string.Compare(a, b, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks whether <paramref name="value"/> exists in a comma-separated list.
        /// </summary>
        private static bool IsIn(string value, string list, StringComparison sc)
        {
            foreach (var item in list.Split(','))
            {
                if (string.Equals(value, item.Trim(), sc))
                    return true;
            }
            return false;
        }
    }
}
