using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Grpc.Net.Client;
using Google.Protobuf;
using ImageProcessingGrpc;

var channel = GrpcChannel.ForAddress("http://localhost:5001", new GrpcChannelOptions
{
    MaxReceiveMessageSize = 64 * 1024 * 1024,
    MaxSendMessageSize = 64 * 1024 * 1024
});

var invoker = channel.CreateCallInvoker();
var client = new ImageProcessor.ImageProcessorClient(invoker);

using var original = new Bitmap("image.jpg");
var imageData = GetImageBytes(original, out var width, out var height);

await ProcessImage("invert", imageData, width, height);

return;

async Task ProcessImage(string operation, byte[] imageData, int width, int height)
{
    var request = new ImageRequest
    {
        Operation = operation,
        ImageData = ByteString.CopyFrom(imageData),
    };
    
    var sw = Stopwatch.StartNew();
    var response = await client.ProcessImageAsync(request);
    sw.Stop();
    
    var result = response.ProcessedImage.ToByteArray();
    var processed = FromImageBytes(result, width, height);
    processed.Save($"output_{operation}.jpg", ImageFormat.Jpeg);
    
    Console.WriteLine($"[DONE] Operation: {operation}; Time: {sw.ElapsedMilliseconds} ms;");
}

byte[] GetImageBytes(Bitmap bmp, out int width, out int height)
{
    width = bmp.Width;
    height = bmp.Height;

    var data = bmp.LockBits(
        new Rectangle(0, 0, bmp.Width, bmp.Height),
        ImageLockMode.ReadOnly,
        PixelFormat.Format24bppRgb);

    int byteCount = Math.Abs(data.Stride) * bmp.Height;
    byte[] bytes = new byte[byteCount];
    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, bytes, 0, byteCount);

    bmp.UnlockBits(data);
    return bytes;
}

Bitmap FromImageBytes(byte[] bytes, int width, int height)
{
    var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
    var data = bmp.LockBits(
        new Rectangle(0, 0, width, height),
        ImageLockMode.WriteOnly,
        PixelFormat.Format24bppRgb);

    System.Runtime.InteropServices.Marshal.Copy(bytes, 0, data.Scan0, bytes.Length);
    bmp.UnlockBits(data);
    return bmp;
}
