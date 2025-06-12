namespace ZTP.Project2.Server.OpenCl;

using OpenCL.Net;

public static class OpenClWrapper
{
    public static byte[] ProcessImage(byte[] imageData, string operation)
    {
        var error = ErrorCode.Success;

        var platform = Cl.GetPlatformIDs(out error).First();
        var device = Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error).First();

        var context = Cl.CreateContext(null, 1, [device], null, IntPtr.Zero, out error);
        var queue = Cl.CreateCommandQueue(context, device, CommandQueueProperties.None, out error);

        var kernelSource = File.ReadAllText("OpenCl\\Kernel.cl");
        var program = Cl.CreateProgramWithSource(context, 1, new[] { kernelSource }, null, out error);
        error = Cl.BuildProgram(program, 0, null, string.Empty, null, IntPtr.Zero);

        var kernel = Cl.CreateKernel(program, operation, out error);

        var inputBuffer = Cl.CreateBuffer(context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, imageData.Length,
            imageData, out error);
        var outputBuffer = Cl.CreateBuffer(context, MemFlags.WriteOnly, imageData.Length, IntPtr.Zero, out error);

        switch (operation)
        {
            case "invert":
                Cl.SetKernelArg(kernel, 0, inputBuffer);
                Cl.SetKernelArg(kernel, 1, imageData.Length);
                break;
        }
        
        var globalWorkSize = new IntPtr[] { (IntPtr)imageData.Length };
        Cl.EnqueueNDRangeKernel(queue, kernel, 1, null, globalWorkSize, null, 0, null, out _);

        Cl.Finish(queue);

        var result = new byte[imageData.Length];
        Cl.EnqueueReadBuffer(queue,
            inputBuffer,
            Bool.True,
            IntPtr.Zero,
            new IntPtr(imageData.Length),
            result, 0, null, out _);


        Cl.ReleaseKernel(kernel);
        Cl.ReleaseProgram(program);
        Cl.ReleaseMemObject(inputBuffer);
        Cl.ReleaseMemObject(outputBuffer);
        Cl.ReleaseCommandQueue(queue);
        Cl.ReleaseContext(context);

        return result;
    }
}