/**
 * Author:  Rakesh Kumar Vali
 * Created: 25.09.2018
 * Summary: Contains helper fuctions
 **/
using System.Text;

namespace BMP2DDS.Source
{
    public class Helper
    {
        /// <summary>
        /// Converts from BGR to RGB and vice versa
        /// </summary>
        /// <param name="imageData">Image data which is of type byte[]</param>
        /// <param name="imageSize">Image size</param>
        /// <returns>Converted byte array</returns>
        public static byte[] BGRToRGB(byte[] imageData, uint imageSize)
        {
            for (ulong i = 0; i < imageSize; i += 3)
            {
                byte tempByte   = 0;
                tempByte        = imageData[i];
                imageData[i]    = imageData[i + 2];     // Swap blue color with red
                imageData[i + 2] = tempByte;
            }
            return imageData;
        }

        /// <summary>
        /// Compress RGB data 
        /// </summary>
        /// <param name="rgb">RGB data to be compressed into integer</param>
        /// <returns>Compressed integer data</returns>
        public static int CompressRGBData(byte[] rgb)
        {

            byte color0 = (byte)(((rgb[1] & 28) << 3) | (rgb[2] >> 3));                                                                             //								red 5 bits			green 3 bits
            byte color1 = (byte)((rgb[0] & 248) | (rgb[1] >> 5));
            return color0 + color1 * 256;
        }

        /// <summary>
        /// Uncompressess RGB data
        /// </summary>
        /// <param name="color">Compressed color data</param>
        /// <returns>Decompressed byte array</returns>
        public static byte[] UnCompressRGBData(int color)
        {

            byte[] rgb = new byte[3];

            rgb[0] = (byte)((color >> 11) & 31);
            rgb[1] = (byte)((color >> 5) & 63);
            rgb[2] = (byte)((color) & 31);

            rgb[0] = (byte)((rgb[0] << 3) | (rgb[0] >> 2));
            rgb[1] = (byte)((rgb[1] << 2) | (rgb[1] >> 4));
            rgb[2] = (byte)((rgb[2] << 3) | (rgb[2] >> 2));

            return rgb;
        }

        public static string ByteToString(byte[] Expression, int StartIndex, int Length)
        {
            return Encoding.Default.GetString(Expression, StartIndex, Length);
        }

        public static byte[] StringToByte(string expression)
        {
            return Encoding.ASCII.GetBytes(expression);
        }
    }
}
