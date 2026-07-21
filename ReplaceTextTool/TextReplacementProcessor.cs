using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace ReplaceTextTool;

internal static class TextReplacementProcessor
{
    private static readonly Regex JavaScriptDecimalRegex = new(
        @"(?<![A-Za-z0-9_.])(?<number>(?:\d+\.\d*|\.\d+)(?:[eE][+-]?\d+)?)(?![A-Za-z0-9_.])",
        RegexOptions.CultureInvariant);

    private static readonly Regex AssignmentStatementRegex = new(
        @"(?:\+\+|--|\+=|-=|\*=|/=|%=|&=|\|=|\^=|\?\?=|<<=|>>=|(?<![=!<>+\-*/%&|^])=(?!=|>))",
        RegexOptions.CultureInvariant);

    private static readonly Regex ControlStatementRegex = new(
        @"^(?:if|for|foreach|while|switch|using|lock|catch)\b",
        RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static ReplacementResult Process(string source, string configurationJson)
    {
        ReplacementConfiguration configuration = JsonSerializer.Deserialize<ReplacementConfiguration>(
            configurationJson,
            JsonOptions) ?? throw new JsonException("The configuration is empty.");

        List<ReplacementRule> rules = configuration.ReplaceRules
            ?? throw new JsonException("The configuration must contain a ReplaceRules array.");

        string result = source;
        int replacementCount = 0;

        for (int index = 0; index < rules.Count; index++)
        {
            ReplacementRule rule = rules[index]
                ?? throw new JsonException($"ReplaceRules[{index}] cannot be null.");

            if (string.IsNullOrEmpty(rule.Text))
            {
                throw new JsonException($"ReplaceRules[{index}].text cannot be empty.");
            }

            if (rule.Replace is null)
            {
                throw new JsonException($"ReplaceRules[{index}].replace is required.");
            }

            if (ContainsWildcard(rule.Text))
            {
                (result, int matches) = ApplyWildcardRule(result, rule.Text, rule.Replace, index);
                replacementCount += matches;
            }
            else
            {
                replacementCount += CountOccurrences(result, rule.Text);
                result = result.Replace(rule.Text, rule.Replace, StringComparison.Ordinal);
            }
        }

        if (configuration.ConvertJavaScriptDecimalsToCSharp)
        {
            (result, int decimalCount) = ConvertJavaScriptDecimals(result);
            replacementCount += decimalCount;
        }

        if (configuration.AddMissingSemicolonTerminators)
        {
            (result, int semicolonCount) = AddMissingSemicolonTerminators(result);
            replacementCount += semicolonCount;
        }

        return new ReplacementResult(result, rules.Count, replacementCount);
    }

    private static (string Result, int ReplacementCount) ConvertJavaScriptDecimals(string source)
    {
        int replacementCount = 0;
        string result = JavaScriptDecimalRegex.Replace(source, match =>
        {
            replacementCount++;
            string number = match.Groups["number"].Value;

            if (number.StartsWith('.'))
            {
                number = $"0{number}";
            }
            else if (number.EndsWith('.'))
            {
                number = $"{number}0";
            }

            return $"{number}M";
        });

        return (result, replacementCount);
    }

    private static (string Result, int ReplacementCount) AddMissingSemicolonTerminators(string source)
    {
        string[] parts = Regex.Split(source, "(\\r\\n|\\n|\\r)");
        int replacementCount = 0;

        // Regex.Split retains line separators in the odd-numbered elements.
        for (int index = 0; index < parts.Length; index += 2)
        {
            string line = parts[index];
            int commentIndex = FindLineCommentStart(line);
            string code = commentIndex >= 0 ? line[..commentIndex] : line;
            string trimmedCode = code.Trim();

            if (!ShouldAddSemicolon(trimmedCode))
            {
                continue;
            }

            int insertionIndex = code.TrimEnd().Length;
            parts[index] = line.Insert(insertionIndex, ";");
            replacementCount++;
        }

        return (string.Concat(parts), replacementCount);
    }

    private static bool ShouldAddSemicolon(string code)
    {
        if (string.IsNullOrEmpty(code)
            || code.StartsWith("//", StringComparison.Ordinal)
            || code.StartsWith("/*", StringComparison.Ordinal)
            || code.StartsWith('*')
            || code.StartsWith('#')
            || ControlStatementRegex.IsMatch(code)
            || !AssignmentStatementRegex.IsMatch(code))
        {
            return false;
        }

        string[] incompleteEndings =
        [
            ";", "{", "}", ":", ",", "=", "+", "-", "*", "/", "%", "&&", "||", "??", ".", "(", "["
        ];

        return !incompleteEndings.Any(ending => code.EndsWith(ending, StringComparison.Ordinal));
    }

    private static int FindLineCommentStart(string line)
    {
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool inTemplateLiteral = false;
        bool escaped = false;

        for (int index = 0; index < line.Length - 1; index++)
        {
            char character = line[index];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character == '\'' && !inDoubleQuote && !inTemplateLiteral)
            {
                inSingleQuote = !inSingleQuote;
            }
            else if (character == '"' && !inSingleQuote && !inTemplateLiteral)
            {
                inDoubleQuote = !inDoubleQuote;
            }
            else if (character == '`' && !inSingleQuote && !inDoubleQuote)
            {
                inTemplateLiteral = !inTemplateLiteral;
            }
            else if (character == '/'
                && line[index + 1] == '/'
                && !inSingleQuote
                && !inDoubleQuote
                && !inTemplateLiteral)
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ContainsWildcard(string text) => text.IndexOfAny(['*', '%']) >= 0;

    private static (string Result, int MatchCount) ApplyWildcardRule(
        string source,
        string searchPattern,
        string replacementPattern,
        int ruleIndex)
    {
        var regexPattern = new StringBuilder();
        var captures = new List<WildcardCapture>();

        for (int index = 0; index < searchPattern.Length; index++)
        {
            char character = searchPattern[index];

            if (character == '*')
            {
                string groupName = $"wildcard{captures.Count}";
                captures.Add(new WildcardCapture(character, groupName));

                // Keep * inside one identifier segment. This prevents a rule such
                // as this.C*.UB1 from starting at one expression and ending at a
                // later expression on the same line.
                regexPattern.Append($"(?<{groupName}>[A-Za-z0-9_]*)");
            }
            else if (character == '%')
            {
                string groupName = $"wildcard{captures.Count}";
                captures.Add(new WildcardCapture(character, groupName));
                regexPattern.Append($"(?<{groupName}>[^\\r\\n])");
            }
            else
            {
                regexPattern.Append(Regex.Escape(character.ToString()));
            }
        }

        ValidateReplacementWildcards(replacementPattern, captures, ruleIndex);

        var regex = new Regex(regexPattern.ToString(), RegexOptions.CultureInvariant);
        int matchCount = 0;
        string result = regex.Replace(source, match =>
        {
            matchCount++;
            return ExpandReplacement(replacementPattern, captures, match);
        });

        return (result, matchCount);
    }

    private static void ValidateReplacementWildcards(
        string replacementPattern,
        List<WildcardCapture> captures,
        int ruleIndex)
    {
        foreach (char symbol in new[] { '*', '%' })
        {
            int available = captures.Count(capture => capture.Symbol == symbol);
            int requested = replacementPattern.Count(character => character == symbol);

            if (requested > available)
            {
                throw new JsonException(
                    $"ReplaceRules[{ruleIndex}].replace contains more '{symbol}' placeholders " +
                    "than its text pattern.");
            }
        }
    }

    private static string ExpandReplacement(
        string replacementPattern,
        List<WildcardCapture> captures,
        Match match)
    {
        var result = new StringBuilder();
        int nextStar = 0;
        int nextPercent = 0;
        WildcardCapture[] stars = captures.Where(capture => capture.Symbol == '*').ToArray();
        WildcardCapture[] percents = captures.Where(capture => capture.Symbol == '%').ToArray();

        foreach (char character in replacementPattern)
        {
            if (character == '*')
            {
                result.Append(match.Groups[stars[nextStar++].GroupName].Value);
            }
            else if (character == '%')
            {
                result.Append(match.Groups[percents[nextPercent++].GroupName].Value);
            }
            else
            {
                result.Append(character);
            }
        }

        return result.ToString();
    }

    private static int CountOccurrences(string value, string searchText)
    {
        int count = 0;
        int startIndex = 0;

        while ((startIndex = value.IndexOf(searchText, startIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            startIndex += searchText.Length;
        }

        return count;
    }

    private readonly record struct WildcardCapture(char Symbol, string GroupName);
}
