using System.Drawing;
using Ztp.Project1.Enums;

namespace Ztp.Project1;

public static class FilterSelector
{
    public static (Func<Bitmap, Bitmap> Filter, string Name) Choose(Configuration cfg)
    {
        return cfg.MemoryAccess switch
        {
            MemoryAccessMode.Managed => (LaplaceFilters.Managed, "managed"),
            MemoryAccessMode.Fixed => (LaplaceFilters.ManagedFixed, "managed_fixed"),
            MemoryAccessMode.Pooled => (LaplaceFilters.UnmanagedPooled, "unmanaged_pooled"),
            MemoryAccessMode.Simd => (LaplaceFilters.ManagedSimd, "simd"),
            _ => (LaplaceFilters.Unmanaged, "unmanaged")
        };
    }
}