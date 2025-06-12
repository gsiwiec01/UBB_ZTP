using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;

namespace Ztp.Project1;

public static class LaplaceFilters
{
    private static readonly int[] Kernel =
    [
        0, 0, -1, 0, 0,
        0, -1, -2, -1, 0,
        -1, -2, 16, -2, -1,
        0, -1, -2, -1, 0,
        0, 0, -1, 0, 0
    ];

    public static Bitmap Managed(Bitmap bmp) => ApplyManaged(bmp, useFixed: false, usePool: false);
    public static Bitmap ManagedFixed(Bitmap bmp) => ApplyManaged(bmp, useFixed: true, usePool: false);
    public static Bitmap Unmanaged(Bitmap bmp) => ApplyUnmanaged(bmp, useFixed: false, usePool: false);
    public static Bitmap UnmanagedFixed(Bitmap bmp) => ApplyUnmanaged(bmp, useFixed: true, usePool: false);
    public static Bitmap UnmanagedPooled(Bitmap bmp) => ApplyUnmanaged(bmp, useFixed: true, usePool: true);
    public static Bitmap ManagedSimd(Bitmap bmp) => ApplyManagedSimd(bmp);

    private static Bitmap ApplyManaged(Bitmap bmp, bool useFixed, bool usePool)
    {
        var w = bmp.Width;
        var h = bmp.Height;
        Bitmap res = new(w, h);

        var len = w * h * 3;
        var src = usePool ? ArrayPool<byte>.Shared.Rent(len) : new byte[len];
        var dst = usePool ? ArrayPool<byte>.Shared.Rent(len) : new byte[len];

        try
        {
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var c = bmp.GetPixel(x, y);
                var i = (y * w + x) * 3;
                src[i] = c.R;
                src[i + 1] = c.G;
                src[i + 2] = c.B;
            }

            if (useFixed) FixedConv(src, dst, w, h);
            else SafeConv(src, dst, w, h);

            for (var y = 2; y < h - 2; y++)
            for (var x = 2; x < w - 2; x++)
            {
                var i = (y * w + x) * 3;
                res.SetPixel(x, y, Color.FromArgb(dst[i], dst[i + 1], dst[i + 2]));
            }

            return res;
        }
        finally
        {
            if (usePool)
            {
                ArrayPool<byte>.Shared.Return(src);
                ArrayPool<byte>.Shared.Return(dst);
            }
        }
    }

    private static Bitmap ApplyUnmanaged(Bitmap bmp, bool useFixed, bool usePool)
    {
        Bitmap res = new(bmp.Width, bmp.Height);

        var srcData = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb
        );

        var dstData = res.LockBits(
            new Rectangle(0, 0, res.Width, res.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format24bppRgb
        );

        int[]? pooledKernel = null;
        if (usePool)
        {
            pooledKernel = ArrayPool<int>.Shared.Rent(Kernel.Length);
            Kernel.CopyTo(pooledKernel, 0);
        }

        try
        {
            unsafe
            {
                var srcPtr = (byte*)srcData.Scan0;
                var dstPtr = (byte*)dstData.Scan0;
                var stride = srcData.Stride;
                var w = bmp.Width;
                var h = bmp.Height;

                fixed (int* kPtrFixed = usePool ? pooledKernel : Kernel)
                {
                    for (var y = 2; y < h - 2; y++)
                    for (var x = 2; x < w - 2; x++)
                    {
                        int r = 0, g = 0, b = 0;

                        for (var ky = -2; ky <= 2; ky++)
                        for (var kx = -2; kx <= 2; kx++)
                        {
                            var p = srcPtr + ((y + ky) * stride) + ((x + kx) * 3);
                            var kv = kPtrFixed[(ky + 2) * 5 + (kx + 2)];
                            b += p[0] * kv;
                            g += p[1] * kv;
                            r += p[2] * kv;
                        }

                        var d = dstPtr + (y * stride) + (x * 3);
                        d[0] = (byte)Math.Clamp(b, 0, 255);
                        d[1] = (byte)Math.Clamp(g, 0, 255);
                        d[2] = (byte)Math.Clamp(r, 0, 255);
                    }
                }
            }
        }
        finally
        {
            if (usePool && pooledKernel is not null)
                ArrayPool<int>.Shared.Return(pooledKernel);

            bmp.UnlockBits(srcData);
            res.UnlockBits(dstData);
        }

        return res;
    }

    private static Bitmap ApplyManagedSimd(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        Bitmap res = new(w, h);

        var pixels = w * h;
        var lenRgb = pixels * 3;

        var src = ArrayPool<short>.Shared.Rent(lenRgb);
        var dst = ArrayPool<short>.Shared.Rent(lenRgb);

        try
        {
            var pos = 0;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var c = bmp.GetPixel(x, y);
                src[pos++] = (short)c.R;
                src[pos++] = (short)c.G;
                src[pos++] = (short)c.B;
            }

            ConvSimd(src, dst, w, h);

            for (var y = 2; y < h - 2; y++)
            for (var x = 2; x < w - 2; x++)
            {
                var i = (y * w + x) * 3;
                res.SetPixel(x, y, Color.FromArgb(
                    (byte)dst[i],
                    (byte)dst[i + 1],
                    (byte)dst[i + 2]));
            }

            return res;
        }
        finally
        {
            ArrayPool<short>.Shared.Return(src);
            ArrayPool<short>.Shared.Return(dst);
        }
    }

    private static unsafe void FixedConv(byte[] input, byte[] output, int wid, int hei)
    {
        fixed (byte* ip = input)
        fixed (byte* op = output)
        fixed (int* kp = Kernel)
        {
            for (var y = 2; y < hei - 2; y++)
            for (var x = 2; x < wid - 2; x++)
            {
                var r = 0;
                var g = 0;
                var b = 0;

                for (var ky = -2; ky <= 2; ky++)
                for (var kx = -2; kx <= 2; kx++)
                {
                    var p = ip + (((y + ky) * wid + (x + kx)) * 3);
                    var kv = kp[(ky + 2) * 5 + (kx + 2)];
                    r += p[0] * kv;
                    g += p[1] * kv;
                    b += p[2] * kv;
                }

                var d = op + ((y * wid + x) * 3);

                d[0] = (byte)Math.Clamp(r, 0, 255);
                d[1] = (byte)Math.Clamp(g, 0, 255);
                d[2] = (byte)Math.Clamp(b, 0, 255);
            }
        }
    }

    private static void SafeConv(byte[] input, byte[] output, int wid, int hei)
    {
        for (var y = 2; y < hei - 2; y++)
        for (var x = 2; x < wid - 2; x++)
        {
            int r = 0, g = 0, b = 0;
            for (var ky = -2; ky <= 2; ky++)
            for (var kx = -2; kx <= 2; kx++)
            {
                var idx = ((y + ky) * wid + (x + kx)) * 3;
                var kv = Kernel[(ky + 2) * 5 + (kx + 2)];
                r += input[idx] * kv;
                g += input[idx + 1] * kv;
                b += input[idx + 2] * kv;
            }

            var o = (y * wid + x) * 3;
            output[o] = (byte)Math.Clamp(r, 0, 255);
            output[o + 1] = (byte)Math.Clamp(g, 0, 255);
            output[o + 2] = (byte)Math.Clamp(b, 0, 255);
        }
    }

    private static void ConvSimd(short[] src, short[] dst, int w, int h)
    {
        var vec = Vector<int>.Count;
        var stride = w * 3; 

        var k = new Vector<int>[25];
        for (var i = 0; i < 25; i++)
            k[i] = new Vector<int>(Kernel[i]);

        for (var y = 2; y < h - 2; y++)
        {
            for (var x = 2; x <= w - 2 - vec; x += vec)
            {
                var sumR = Vector<int>.Zero;
                var sumG = Vector<int>.Zero;
                var sumB = Vector<int>.Zero;
                var kernelIdx = 0;

                for (var ky = -2; ky <= 2; ky++)
                {
                    var srcRow = (y + ky) * stride;
                    for (var kx = -2; kx <= 2; kx++, kernelIdx++)
                    {
                        var baseOff = srcRow + (x + kx) * 3;

                        var r16 = new Vector<short>(src, baseOff + 0);
                        var g16 = new Vector<short>(src, baseOff + 1);
                        var b16 = new Vector<short>(src, baseOff + 2);

                        Vector.Widen(r16, out var rLo, out var rHi);
                        Vector.Widen(g16, out var gLo, out var gHi);
                        Vector.Widen(b16, out var bLo, out var bHi);

                        var r = Vector.BitwiseOr(rLo, rHi);
                        var g = Vector.BitwiseOr(gLo, gHi);
                        var b = Vector.BitwiseOr(bLo, bHi);

                        var kv = k[kernelIdx];
                        sumR += r * kv;
                        sumG += g * kv;
                        sumB += b * kv;
                    }
                }

                var zero = Vector<int>.Zero;
                var max = new Vector<int>(255);
                sumR = Vector.Min(Vector.Max(sumR, zero), max);
                sumG = Vector.Min(Vector.Max(sumG, zero), max);
                sumB = Vector.Min(Vector.Max(sumB, zero), max);

                for (var i = 0; i < vec; i++)
                {
                    var dstOff = (y * w + (x + i)) * 3;
                    dst[dstOff + 0] = (short)sumR[i];
                    dst[dstOff + 1] = (short)sumG[i];
                    dst[dstOff + 2] = (short)sumB[i];
                }
            }
        }
    }
}