using System.Runtime;
using System.Text;
using Microsoft.Extensions.Configuration;
using Ztp.Project1.Enums;

namespace Ztp.Project1;

public class Configuration
{
    public string ImagePath { get; init; }
    public int NumOfIterations { get; init; }
    public bool DisposeBitmaps { get; init; }
    public bool UseParallel { get; init; }
    public MemoryAccessMode MemoryAccess { get; init; } = MemoryAccessMode.Managed;
    public GcMode GcMode { get; init; } = GcMode.None;
    public GcLatency GcLatencyMode { get; init; } = GcLatency.Default;

    public static Configuration Load(string profile, IConfiguration configuration)
    {
        var section = configuration.GetSection($"Profiles:{profile}");
        if (!section.Exists())
            throw new InvalidOperationException($"Profile '{profile}' does not exist in the configuration.");
        
        var cfg = section.Get<Configuration>();
        if (cfg == null)
            throw new InvalidOperationException($"Failed to load configuration for profile '{profile}'.");
        
        return cfg;
    }

    public void DumpToConsole()
    {
        var sb = new StringBuilder();

        sb.AppendLine("====================== Configuration ======================");
        sb.AppendLine($"ImagePath: {ImagePath}");
        sb.AppendLine($"Profile: {nameof(Configuration)}");
        sb.AppendLine("===========================================================");

        sb.AppendLine($"ImagePath             : {ImagePath}");
        sb.AppendLine($"NumOfIterations       : {NumOfIterations}");
        sb.AppendLine($"MemoryAccess          : {MemoryAccess}");
        sb.AppendLine($"GcMode                : {GcMode}");
        sb.AppendLine($"GcLatencyMode         : {GcLatencyMode}");
        sb.AppendLine($"DisposeBitmaps        : {DisposeBitmaps}");
        sb.AppendLine($"UseParallel           : {UseParallel}");
        sb.AppendLine("===========================================================");
        
        Console.WriteLine(sb.ToString());
    }

    public void ApplyGcConfig()
    {
        if (GcMode == GcMode.CollectAndCompact)
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        
        GCSettings.LatencyMode = GcLatencyMode switch
        {
            GcLatency.LowLatency => GCLatencyMode.LowLatency,
            GcLatency.Sustained => GCLatencyMode.SustainedLowLatency,
            _ => GCSettings.LatencyMode
        };
    }
}