/**
 * Author:  Rakesh Kumar Vali
 * Created: 24.09.2018
 * Summary: BMP file format details
 **/

namespace BMP2DDS.Source
{
    public class BMPFormat
    {
        public static int       biRGB = 0x0000;			// The bitmap is in uncompressed red green blue (RGB) format that is not compressed and does not use color masks.
        public static int       piPerMeter = 2835;	// 72 DPI × 39.3701 inches per meter yields 2834.6472
    }

    public struct BMPFileHeader
    {
        public string   bfType;             // Number for file
        public uint     bfSize;             // Size of file
        public ushort   bfReserved1;        // Reserved
        public ushort   bfReserved2;        // Reserved
        public uint     bfOffBits;          // Offset to bitmap data
    }

    public struct BMPInfoHeader
    {
        public uint     biSize;             // Size of info header
        public int      biWidth;            // Width of image
        public int      biHeight;           // Height of image
        public ushort   biPlanes;           // Number of color planes
        public ushort   biBitCount;         // Number of bits per pixel
        public uint     biCompression;      // Type of compression to use
        public uint     biSizeImage;        // Size of image data
        public int      biXPelsPerMeter;    // X pixels per meter
        public int      biYPelsPerMeter;    // Y pixels per meter
        public uint     biClrUsed;          // Number of colors used
        public uint     biClrImportant;     // Number of important colors
    }
}
