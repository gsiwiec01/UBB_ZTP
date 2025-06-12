using System.Runtime.InteropServices;

namespace ZTP.Project2.Server;

public static class CudaInterop
{
    [DllImport("cuda_processor.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ProcessGrayscale(byte[] input, int length, byte[] output);
}