using System.Diagnostics;
using System.Drawing;
using Ztp.Project1.Enums;

namespace Ztp.Project1;

internal static class ImageProcessor
{
    public static void Process(
        string imagePath,
        Func<Bitmap, Bitmap> filter,
        string filterName,
        Configuration settings)
    {
        ArgumentNullException.ThrowIfNull(imagePath);

        var sw = Stopwatch.StartNew();

        if (settings.UseParallel)
        {
            Parallel.For(
                0,
                settings.NumOfIterations,
                p => ProcessSingle(imagePath, filter, filterName, settings)
            );
        }
        else
        {
            for (var i = 0; i < settings.NumOfIterations; i++)
            {
                ProcessSingle(imagePath, filter, filterName, settings);
            }
        }

        sw.Stop();

        Console.WriteLine($"Processed {imagePath} (filter: {filterName}) in {sw.ElapsedMilliseconds}ms");
    }

    private static void ProcessSingle(string imagePath, Func<Bitmap, Bitmap> flt, string filterName,
        Configuration stg)
    {
        Bitmap? src = null;
        Bitmap? dst = null;

        try
        {
            src = new Bitmap(imagePath);
            dst = flt(src);
        }
        finally
        {
            if (stg.DisposeBitmaps)
            {
                src?.Dispose();
                dst?.Dispose();
            }

            if (stg.GcMode == GcMode.Collect)
                GC.Collect();
        }
    }
}