using System.Text.Json.Serialization;

namespace ReplaceTextTool;

internal sealed class ReplacementConfiguration
{
    [JsonPropertyName("ConvertJavaScriptDecimalsToCSharp")]
    public bool ConvertJavaScriptDecimalsToCSharp { get; init; }

    [JsonPropertyName("AddMissingSemicolonTerminators")]
    public bool AddMissingSemicolonTerminators { get; init; }

    [JsonPropertyName("ReplaceRules")]
    public List<ReplacementRule>? ReplaceRules { get; init; }
}

internal sealed class ReplacementRule
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("replace")]
    public string? Replace { get; init; }
}

internal readonly record struct ReplacementResult(string Text, int RuleCount, int ReplacementCount);
