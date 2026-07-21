namespace FIGSignalExSvc.Plugins;

internal sealed record StudyPluginManifest
{
    public string ColType { get; init; } = "";
    public string Assembly { get; init; } = "";
    public int ApiVersion { get; init; }
    public string Version { get; init; } = "";
}
