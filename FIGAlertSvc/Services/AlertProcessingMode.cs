using FIGCommon.Models.Alert;

namespace FIGAlertSvc.Services;

public enum AlertProcessingMode { Combined, SystemOnly, OtherOnly }

public static class AlertProcessingPolicy
{
    public static AlertProcessingMode Read(IConfiguration configuration)
    {
        var value = configuration["AlertProcessing:Mode"] ?? "Combined";
        if (!Enum.TryParse<AlertProcessingMode>(value, true, out var mode) || !Enum.IsDefined(mode))
            throw new InvalidOperationException($"Invalid AlertProcessing:Mode '{value}'. Use Combined, SystemOnly or OtherOnly.");
        return mode;
    }

    public static bool Accepts(AlertProcessingMode mode, bool isSystemAlert) =>
        mode == AlertProcessingMode.Combined || (mode == AlertProcessingMode.SystemOnly) == isSystemAlert;

    public static int CategoryFilter(AlertProcessingMode mode) => mode switch
    {
        AlertProcessingMode.SystemOnly => 1,
        AlertProcessingMode.OtherOnly => 0,
        _ => -1
    };
}
