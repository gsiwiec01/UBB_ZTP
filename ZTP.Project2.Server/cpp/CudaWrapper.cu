#include "CUDAWrapper.h"
#include <cuda_runtime.h>
#include <iostream>

__global__ void grayscaleKernel(unsigned char* input, unsigned char* output, int pixelCount) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i < pixelCount) {
        int idx = i * 3;
        unsigned char r = input[idx];
        unsigned char g = input[idx + 1];
        unsigned char b = input[idx + 2];
        unsigned char gray = static_cast<unsigned char>((r + g + b) / 3);
        output[idx] = gray;
        output[idx + 1] = gray;
        output[idx + 2] = gray;
    }
}

void ProcessGrayscale(unsigned char* input, int length, unsigned char* output) {
    int totalPixels = length / 3;
    int totalBytes = length;

    unsigned char* devInput;
    unsigned char* devOutput;

    cudaMalloc((void**)&devInput, totalBytes);
    cudaMalloc((void**)&devOutput, totalBytes);

    cudaMemcpy(devInput, input, totalBytes, cudaMemcpyHostToDevice);

    int threadsPerBlock = 256;
    int blocks = (totalPixels + threadsPerBlock - 1) / threadsPerBlock;

    grayscaleKernel<<<blocks, threadsPerBlock>>>(devInput, devOutput, totalPixels);

    cudaMemcpy(output, devOutput, totalBytes, cudaMemcpyDeviceToHost);

    cudaFree(devInput);
    cudaFree(devOutput);
}
