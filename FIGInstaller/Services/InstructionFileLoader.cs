using System.Text.Json;
using FIGInstaller.Models;

namespace FIGInstaller.Services;

public sealed class InstructionFileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public InstallationInstructions Load(string instructionFile)
    {
        if (!File.Exists(instructionFile))
        {
            throw new FileNotFoundException($"Installation instruction file was not found: {instructionFile}");
        }

        try
        {
            string json = File.ReadAllText(instructionFile);
            return JsonSerializer.Deserialize<InstallationInstructions>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Installation instruction file is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Installation instruction file is not valid JSON: {ex.Message}", ex);
        }
    }
}
