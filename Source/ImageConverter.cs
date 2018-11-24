/**
 * Author:  Rakesh Kumar Vali
 * Created: 24.09.2018
 * Summary: Responsible for converting images from bmp to dds and dds to bmp.
 **/

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BMP2DDS.Source
{ 

    public class ImageConverter
    {
        byte[]          ddsPixelData;       // dds image data to be written to file
        byte[]          bmpPixelData;       // bmp image data to be written to file
        DDSHeader       ddsHeader;          // dds header details
        BMPFileHeader   bmpFileHeader;      // bmp file header details
        BMPInfoHeader   bmpInfoHeader;      // bmp info header details

        #region BMP To DDS Convertion

        /// <summary>
        /// Converts the BMP image to DDS
        /// </summary>
        /// <param name="bmpPixelData">Uncompressed bmp image data</param>
        /// <param name="imageSize">Total image data size count</param>
        /// <param name="imageWidth">Image width</param>
        /// <param name="imageHeight">Image height</param>
        public void ConvertBMPToDDS(byte[] bmpPixelData, uint imageSize, uint imageWidth, uint imageHeight)
        {
            // Populate dds pixel format details
            DDSPixelFormat pixelFormat;
            pixelFormat.dwsize          = 32;
            pixelFormat.dwflags         = DDSFormat.ddpf;
            pixelFormat.dwfourCC        = DDSFormat.dxt1;
            pixelFormat.dwRGBBitCount   = 0;
            pixelFormat.dwRBitMask      = 0;
            pixelFormat.dwGBitMask      = 0;
            pixelFormat.dwBBitMask      = 0;
            pixelFormat.dwABitMask      = 0;

            // Populate dds header details
            ddsHeader.dwSize                = 124;
            ddsHeader.dwFlags               = DDSFormat.ddsFlags;
            ddsHeader.dwHeight              = imageHeight;
            ddsHeader.dwWidth               = imageWidth;
            ddsHeader.dwPitchOrLinearSize   = Math.Max(1, (imageWidth + 3) / 4) * Math.Max(1, (imageHeight + 3) / 4) * 8;
            ddsHeader.dwDepth               = 0;
            ddsHeader.dwMipMapCount         = 0;
            ddsHeader.dwReserved1           = new uint[11];
            ddsHeader.ddspf                 = pixelFormat;
            ddsHeader.dwCaps                = DDSFormat.ddsTexture;
            ddsHeader.dwCaps2               = 0;
            ddsHeader.dwCaps3               = 0;
            ddsHeader.dwCaps4               = 0;
            ddsHeader.dwReserved2           = 0;

            // Convert BGR to RGB in the image data
            var convertedRGBData = Helper.BGRToRGB(bmpPixelData, imageSize);

            // Compress the image data to DXT1 format
            ddsPixelData = CompressToDXT1(convertedRGBData, ddsHeader.dwPitchOrLinearSize, imageSize, imageWidth, imageHeight);

            // Create the final DXT1 compressed DDS file
            CreateDDSFile();
        }

        /// <summary>
        /// Compress the uncompressed BMP data to DXT1 compression
        /// </summary>
        /// <param name="bmpPixelData">Uncompressed RGB converted image data</param>
        /// <param name="finalImageSize">Final image size</param>
        /// <param name="initialImageSize">Initial image size</param>
        /// <param name="imageWidth">Image width</param>
        /// <param name="imageHeight">Image height</param>
        /// <returns></returns>
        private byte[] CompressToDXT1(byte[] bmpPixelData, uint finalImageSize, uint initialImageSize, uint imageWidth, uint imageHeight)
        {
            // Find number of blocks by dividing initial image size by pixel in block and no. of RGB bytes per pixel
            uint noOfBlocks = initialImageSize / (DDSFormat.pixelsInBlock * DDSFormat.noOfRGBBytes);
            // Initialize compressed data
            byte[] imageData = new byte[finalImageSize];
            // Block indexes
            int blockX = 0;
            int blockY = 0;

            for (int blockIndex = 0; blockIndex < noOfBlocks; blockIndex++, blockX++)
            {
                // Find current block
                if (blockIndex != 0 && blockIndex % (imageWidth / DDSFormat.pixelsInBlockRow) == 0)
                {
                    blockX = 0;
                    blockY++;
                }

                // Iterate through the pixels available inside the block to find the minimum and maximum reference colors
                // 16bit color values
                int minColor  = 65535;  
                int maxColor  = 0;     
                byte[] color0 = { 0, 0, 0 };        // maximum reference color
                byte[] color1 = { 255, 255, 255 };  // minimum reference color
                
                // Iterate through pixel data from bmpPixelData to save it into minimum and maximum colors
                for (int indexY = 0; indexY < 4; indexY++)
                {
                    for (int indexX = 0; indexX < 4; indexX++)
                    {

                        // Find the index from where the color data has to be retrieved from bmpPixelData
                        long bytePos = DDSFormat.noOfRGBBytes * (blockY * DDSFormat.pixelsInBlockRow * imageWidth + blockX * DDSFormat.pixelsInBlockRow + indexY * imageWidth + indexX);
                        // Gather RGB data from the respective index in bmpPixelData
                        byte[] rgb = { bmpPixelData[bytePos], bmpPixelData[bytePos + 1], bmpPixelData[bytePos + 2] };
                        // Compress RGB byte array into an integer to find whether it is of maximum or minimum color
                        int compressedRGB = Helper.CompressRGBData(rgb);

                        // Populate maximum color with RGB data
                        if (compressedRGB > maxColor)
                        {
                            maxColor  = compressedRGB;
                            color0[0] = rgb[0];         // Red
                            color0[1] = rgb[1];         // Green
                            color0[2] = rgb[2];         // Blue
                        }
                        // Populate minimum color with RGB data
                        if (compressedRGB < minColor)
                        {
                            minColor  = compressedRGB;
                            color1[0] = rgb[0];         // Red
                            color1[1] = rgb[1];         // Green
                            color1[2] = rgb[2];         // Blue
                        }
                    }
                }
                
                // Collect total Y blocks and X blocks. Then inverse the block placements 
                uint noOfYBlocks = imageHeight / DDSFormat.pixelsInBlockRow; 
                uint noOfXBlocks = imageWidth / DDSFormat.pixelsInBlockRow;
                // Calculate byte position in the compressed block by using X and Y index
                long compressBytePos = DDSFormat.bytesInCompressedBlock * (noOfYBlocks * noOfXBlocks - blockY * noOfXBlocks - noOfXBlocks + blockX);

                // Calculate the bytes in compressed from the referenced color
                imageData[compressBytePos++] = (byte)(((color0[1] & 28) << 3) | (color0[2] >> 3));      // lowest value in maximum color
                imageData[compressBytePos++] = (byte)((color0[0] & 248) | (color0[1] >> 5));            // highest value in maximum color
                imageData[compressBytePos++] = (byte)(((color1[1] & 28) << 3) | (color1[2] >> 3));      // lowest value in minimum reference color
                imageData[compressBytePos++] = (byte)(((color1[0] & 248) | color1[1] >> 5));            // highest value in minimum reference color

                byte[] color2 = { 0, 0, 0 };
                byte[] color3 = { 0, 0, 0 };

                // If maxColor is greater than minColor then use linear interpolation and populate both color2 and color3
                if (maxColor > minColor)
                {
                    color2[0] = (byte)((2 * color0[0] + color1[0]) / 3);
                    color2[1] = (byte)((2 * color0[1] + color1[1]) / 3);
                    color2[2] = (byte)((2 * color0[2] + color1[2]) / 3);

                    color3[0] = (byte)((color0[0] + color1[0] * 2) / 3);
                    color3[1] = (byte)((color0[1] + color1[1] * 2) / 3);
                    color3[2] = (byte)((color0[2] + color1[2] * 2) / 3);
                }
                else // else populate only in color2
                {
                    color2[0] = (byte)((color0[0] + color1[0]) / 2);
                    color2[1] = (byte)((color0[1] + color1[1]) / 2);
                    color2[2] = (byte)((color0[2] + color1[2]) / 2);
                }

                //Loop through pixel colors of block to map colors to reference colors
                for (int indexY = 3; indexY >= 0; indexY--)
                {
                    byte codeByte = 0;
                    for (int indexX = 3; indexX >= 0; indexX--)
                    {
                        // Find the index from where the color data has to be retrieved from bmpPixelData
                        long bytePos = DDSFormat.noOfRGBBytes * (blockY * DDSFormat.pixelsInBlockRow * imageWidth + blockX * DDSFormat.pixelsInBlockRow + indexY * imageWidth + indexX);
                        // Gather RGB data from the respective index in bmpPixelData
                        byte[] rgb = { bmpPixelData[bytePos], bmpPixelData[bytePos + 1], bmpPixelData[bytePos + 2] };
                        int code = 0;

                        // Find position of reference color0 and color2
                        int distanceCo0 = Math.Abs((color0[2] + 256 * (color0[1] + 256 * color0[0])) - (rgb[2] + 256 * (rgb[1] + 256 * rgb[0])));
                        int distanceCo2 = Math.Abs((color2[2] + 256 * (color2[1] + 256 * color2[0])) - (rgb[2] + 256 * (rgb[1] + 256 * rgb[0])));

                        // if color0 is greater than color2 then calculate position of reference color3 and pass code value to 2
                        if (distanceCo0 > distanceCo2)
                        {
                            code = 2;
                            int distanceCo3 = Math.Abs((color3[2] + 256 * (color3[1] + 256 * color3[0])) - (rgb[2] + 256 * (rgb[1] + 256 * rgb[0])));
                            // if color2 is greater than color3 then calculate position for reference color1 and pass code value to 3
                            if (distanceCo2 > distanceCo3)
                            {
                                code = 3;
                                int distanceCo1 = Math.Abs((color1[2] + 256 * (color1[1] + 256 * color1[0])) - (rgb[2] + 256 * (rgb[1] + 256 * rgb[0])));
                                // if color3 is greater than color1 then pass code value to 1
                                if (distanceCo3 > distanceCo1)
                                {
                                    code = 1;
                                }
                            }
                        }
                        
                        // Form byte of 2 bit codes, pixels a to e are in order MSB e -> a LSB
                        codeByte = (byte)(codeByte | (byte)((code & 3) << 2 * indexX));
                    }

                    // Assign the calculated byte to compressed image data
                    imageData[compressBytePos++] = codeByte;
                }
            }
            return imageData;
        }
        
        /// <summary>
        /// Writes compressed DDS data along with header details to a file
        /// </summary>
        private void CreateDDSFile()
        {
            Console.Write("Please enter the file name without extention for the DDS image : ");
            var fileName = Console.ReadLine();
            // If user provides a name with extention then remove the extention
            if(fileName.Contains("."))
            {
                fileName = fileName.Split('.')[0];
            }
            try
            {
                var filePath = Directory.GetCurrentDirectory() + "\\" + fileName + ".dds";
                // If file exists then truncate the previous data and write new data.. else just create one
                FileStream ddsFile = new FileStream(filePath, (File.Exists(filePath)) ? FileMode.Truncate : FileMode.Append);

                //Finalize and write dds header details to file
                ddsFile.Write(Helper.StringToByte("DDS \n"), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwSize), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwFlags), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwHeight), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwWidth), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwPitchOrLinearSize), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwDepth), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwMipMapCount), 0, 4);
                ddsFile.Write(ddsHeader.dwReserved1.SelectMany(BitConverter.GetBytes).ToArray(), 0, 44);

                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwsize), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwflags), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwfourCC), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwRGBBitCount), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwRBitMask), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwGBitMask), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwBBitMask), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.ddspf.dwABitMask), 0, 4);

                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwCaps), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwCaps2), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwDepth), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwCaps3), 0, 4);
                ddsFile.Write(BitConverter.GetBytes(ddsHeader.dwReserved2), 0, 4);

                // Write image data to the file
                ddsFile.Write(ddsPixelData, 0, (int)ddsHeader.dwPitchOrLinearSize);
                ddsFile.Close();
            }
            catch(IOException msg)
            {
                Console.WriteLine(msg);
                return;
            }
            Console.WriteLine("Image Converted succesfully\n");
            Console.WriteLine(Directory.GetCurrentDirectory() + "\\" + fileName + ".dds\n");
        }
        #endregion

        #region DDS To BMP Convertion

        /// <summary>
        /// Converts DDS image to BMP
        /// </summary>
        /// <param name="ddsPixelData">DXT1 Compressed DDS image data</param>
        /// <param name="imageWidth">Image width</param>
        /// <param name="imageHeight">Image height</param>
        /// <param name="imageSize">Total image size</param>
        public void DDSToBMPConvertion(byte[] ddsPixelData, int imageWidth, int imageHeight, uint imageSize)
        {
            // Populate BMP info and file headers
            bmpFileHeader.bfType = "BM";
            bmpFileHeader.bfSize = imageSize + 54;
            bmpFileHeader.bfReserved1 = 0;
            bmpFileHeader.bfReserved2 = 0;
            bmpFileHeader.bfOffBits = 54;

            bmpInfoHeader.biSize = 40;
            bmpInfoHeader.biWidth = imageWidth;
            bmpInfoHeader.biHeight = imageHeight;
            bmpInfoHeader.biPlanes = 1;
            bmpInfoHeader.biBitCount = 24;
            bmpInfoHeader.biCompression = (ushort)BMPFormat.biRGB;
            bmpInfoHeader.biSizeImage = imageSize;
            bmpInfoHeader.biXPelsPerMeter = BMPFormat.piPerMeter;
            bmpInfoHeader.biYPelsPerMeter = BMPFormat.piPerMeter;
            bmpInfoHeader.biClrUsed = 0;
            bmpInfoHeader.biClrImportant = 0;

            // Uncompress the DXT1 compressed image data
            bmpPixelData = UncompressDXT1Data(ddsPixelData, imageWidth, imageHeight, imageWidth * imageHeight * 3);
            // Create the final uncompressed BMP file
            CreateBMPFile();
        }

        /// <summary>
        /// Uncompress DXT1 compressed DDS data
        /// </summary>
        /// <param name="ddsPixelData">Compressed DDS image data</param>
        /// <param name="imageWidth">Image width</param>
        /// <param name="imageHeight">Image height</param>
        /// <param name="imageSize">Total image size</param>
        /// <returns></returns>
        private byte[] UncompressDXT1Data(byte[] ddsPixelData, int imageWidth, int imageHeight, int imageSize)
        {
            // Initialize uncompressed data
            byte[] imageData = new byte[imageSize];
            uint index = 0;

            // Manipulate image data by inversion so that image will be also be inverted
            for (int indexY = imageHeight / 4 - 1; indexY >= 0; indexY--)
            {
                for (uint indexX = 0; indexX < imageWidth / 4; indexX++)
                {

                    //Extract reference colors from compressed image data
                    byte maxRefHi = ddsPixelData[index++];
                    byte maxRefLo = ddsPixelData[index++];
                    byte minRefLo = ddsPixelData[index++];
                    byte minRefHi = ddsPixelData[index++];

                    // Extract block pixel codes from compressed image data
                    byte blockPixel0 = ddsPixelData[index++];
                    byte blockPixel1 = ddsPixelData[index++];
                    byte blockPixel2 = ddsPixelData[index++];
                    byte blockPixel3 = ddsPixelData[index++];

                    // Calculate 32bit instance of the blocks
                    int bits32 = blockPixel0 + 256 * (blockPixel1 + 256 * (blockPixel2 + 256 * blockPixel3));

                    // Calculate the Maximum and minimum colors used
                    int co0 = maxRefHi + maxRefLo * 256;
                    int co1 = minRefLo + minRefHi * 256;

                    // UnCompress RGB values
                    byte[] color0 = Helper.UnCompressRGBData(co0);
                    byte[] color1 = Helper.UnCompressRGBData(co1);
                    byte[] color2 = new byte[3];
                    byte[] color3 = new byte[3];

                    // If maximum color is greater than minimum, interpolate mid colors
                    if (co0 > co1)
                    {
                        color2[0] = (byte)((2 * color0[0] + color1[0]) / 3);
                        color2[1] = (byte)((2 * color0[1] + color1[1]) / 3);
                        color2[2] = (byte)((2 * color0[2] + color1[2]) / 3);

                        color3[0] = (byte)((color0[0] + 2 * color1[0]) / 3);
                        color3[1] = (byte)((color0[1] + 2 * color1[1]) / 3);
                        color3[2] = (byte)((color0[2] + 2 * color1[2]) / 3);
                    }
                    else // Else convert 3rd color as alpha value
                    { 
                        color2[0] = (byte)((color0[0] + color1[0]) / 2);
                        color2[1] = (byte)((color0[1] + color1[1]) / 2);
                        color2[2] = (byte)((color0[2] + color1[2]) / 2);

                        color3[0] = 0;
                        color3[1] = 0;
                        color3[2] = 0;
                    }

                    // Iterate through color data to build uncompressed data
                    for (int invertYi = 3; invertYi >= 0; invertYi--)
                    {
                        for (int invertXi = 3, xi = 0; invertXi >= 0; invertXi--, xi++)
                        {
                            // Calculate bit position to find the code value for uncompression
                            int bitPos = 31 - (2 * (invertYi * 4 + invertXi) + 1);
                            int code = ((bits32 >> bitPos) & 3);
                            byte[] rgb = new byte[3];

                            // Extract image data based on code value
                            switch (code)
                            {
                                case 0:
                                    rgb[0] = color0[0];
                                    rgb[1] = color0[1];
                                    rgb[2] = color0[2];
                                    break;
                                case 1:
                                    rgb[0] = color1[0];
                                    rgb[1] = color1[1];
                                    rgb[2] = color1[2];
                                    break;
                                case 2:
                                    rgb[0] = color2[0];
                                    rgb[1] = color2[1];
                                    rgb[2] = color2[2];
                                    break;
                                case 3:
                                    rgb[0] = color3[0];
                                    rgb[1] = color3[1];
                                    rgb[2] = color3[2];
                                    break;
                                default:
                                    Console.WriteLine("Invalid code value generated while uncompressing the image");
                                    break;
                            }

                            // Assign the extracted RGB data to uncompressed data(imageData)
                            long byteBufferPos = 3 * (imageWidth * (4 * indexY + invertYi) + 4 * indexX + xi);
                            imageData[byteBufferPos] = rgb[0];        
                            imageData[byteBufferPos + 1] = rgb[1];    
                            imageData[byteBufferPos + 2] = rgb[2];    
                        }
                    }
                }
            }

            //linear rgb buffer
            return imageData;
        }

        /// <summary>
        /// Writes uncompressed BMP data along with header details to a file
        /// </summary>
        void CreateBMPFile()
        {
            Console.Write("Please enter the file name without extention for the BMP image : ");
            var fileName = Console.ReadLine();
            // If user provides a name with extention then remove the extention
            if (fileName.Contains("."))
            {
                fileName = fileName.Split('.')[0];
            }
            try
            {
                var filePath = Directory.GetCurrentDirectory() + "\\" + fileName + ".bmp";
                // If file exists then truncate the previous data and write new data.. else just create one
                FileStream bmpFile = new FileStream(filePath, (File.Exists(filePath)) ? FileMode.Truncate : FileMode.Append);

                //Finalize and write file and info headers to file
                bmpFile.Write(BitConverter.GetBytes(19778), 0, 2);
                bmpFile.Write(BitConverter.GetBytes(bmpFileHeader.bfSize), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpFileHeader.bfReserved1), 0, 2);
                bmpFile.Write(BitConverter.GetBytes(bmpFileHeader.bfReserved2), 0, 2);
                bmpFile.Write(BitConverter.GetBytes(bmpFileHeader.bfOffBits), 0, 4);

                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biSize), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biWidth), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biHeight), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biPlanes), 0, 2);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biBitCount), 0, 2);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biCompression), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biSizeImage), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biXPelsPerMeter), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biYPelsPerMeter), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biClrUsed), 0, 4);
                bmpFile.Write(BitConverter.GetBytes(bmpInfoHeader.biClrImportant), 0, 4);

                // Convert RGB data to BGR
                bmpPixelData = Helper.BGRToRGB(bmpPixelData, bmpInfoHeader.biSizeImage);

                // Write image data to the file
                bmpFile.Write(bmpPixelData, 0, (int)bmpInfoHeader.biSizeImage);
                bmpFile.Close();
            }
            catch(IOException msg)
            {
                Console.WriteLine(msg);
                return;
            }
            Console.WriteLine("Image Converted succesfully\n");
            Console.WriteLine(Directory.GetCurrentDirectory() + "\\" + fileName + ".bmp\n");
        }

#endregion

    }
}
