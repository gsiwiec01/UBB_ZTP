using ZTP.Project2.Server.OpenCl;

namespace ZTP.Project2.Server.Services;

using Grpc.Core;
using Google.Protobuf;
using ImageProcessingGrpc;

public class ImageProcessingService : ImageProcessor.ImageProcessorBase
{
    public override Task<ImageResponse> ProcessImage(ImageRequest request, ServerCallContext context)
    {
        var inputData = request.ImageData.ToByteArray();
        var result = OpenClWrapper.ProcessImage(inputData, request.Operation);

        return Task.FromResult(new ImageResponse
        {
            ProcessedImage = ByteString.CopyFrom(result)
        });
    }
}
