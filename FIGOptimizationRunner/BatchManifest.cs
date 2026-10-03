using FIGOptimizationRunner.Configuration;

namespace FIGOptimizationRunner;

public sealed record CandidateFile(string FileName, string SHA256);

public sealed class BatchManifest
{
    public int Version { get; set; } = 1;
    public string Status { get; set; } = "Preparing";
    public string Generator { get; set; } = "";
    public string Runtime { get; set; } = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
    public DateTimeOffset CreatedUTC { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedUTC { get; set; }
    public DateTimeOffset? FinishedUTC { get; set; }
    public int Attempts { get; set; }
    public string? ExecutableSHA256 { get; set; }
    public string? ReportDirectory { get; set; }
    public string? Error { get; set; }
    public SearchConfiguration Configuration { get; set; } = new();
    public List<CandidateFile> Candidates { get; set; } = [];
}
