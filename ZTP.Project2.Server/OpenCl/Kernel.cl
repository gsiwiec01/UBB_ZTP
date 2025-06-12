__kernel void invert(__global uchar* image, const int length)
{
    int i = get_global_id(0);
    if (i < length) {
        image[i] = 255 - image[i];
    }
}






