/**
 * Author:  Rakesh Kumar Vali
 * Created: 24.09.2018
 * Summary: Loads .bmp / .dds files from specified path. 
 **/
using System;
using System.IO;

namespace BMP2DDS.Source
{
    public class ImageLoader
    {
        private string          imagePath;          // Image path from disk
        private FileStream      imgData;            // Stores raw image data from disk
        private BMPFileHeader   bmpFileHeader;      // BMP File header object
        private BMPInfoHeader   bmpInfoHeader;      // BMP Info header object
        private DDSHeader       ddsHeader;          // DDS header object
        private byte[]          bmpPixelData;       // bmp pixel data loaded from imgData
        private byte[]          ddsPixelData;       // dds pixel data loaded from imgData
        private ImageConverter  imgConverter;       // ImageConverter object

        /// <summary>
        /// Constructor for ImageLoader
        /// </summary>
        /// <param name="filePath">FPath of the file to be loaded</param>
        public ImageLoader(string filePath)
        {
            imagePath       = filePath;
            imgConverter    = new ImageConverter();
        }

        /// <summary>
        /// Function to load the image from the path provided
        /// </summary>
        public void LoadImageFromPath()
        {
            LoadImage();
        }

        /// <summary>
        /// Local function to load the image from the path
        /// </summary>
        private void LoadImage()
        {
            try
            {
                // Load file data to filestream
                imgData = new FileStream(imagePath, FileMode.Open);
                ReadImageData();
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Reads the image data from the Filestream and calls the respective loader
        /// </summary>
        private void ReadImageData()
        {
            var fileFormat = imagePath.Split('.')[1];
            if (fileFormat.ToLower() == "bmp")
            {
                // Load BMP data from the filestream
                if (!LoadBMPData())
                    return;

                // Convert BMP to DDS
                imgConverter.ConvertBMPToDDS(bmpPixelData, bmpInfoHeader.biSizeImage, (uint)bmpInfoHeader.biWidth, (uint)bmpInfoHeader.biHeight);
            }
            else if (fileFormat.ToLower() == "dds")
            {
                // Load DDS data from the filestream
                if (!LoadDDSData())
                    return;

                // Convert DDS to BMP
                var finalImageSize = ddsHeader.dwWidth* ddsHeader.dwHeight * 3;
                imgConverter.DDSToBMPConvertion(ddsPixelData, (int)ddsHeader.dwWidth, (int)ddsHeader.dwHeight, finalImageSize);
            }
            else // if a file different from BMP or DDS is loaded
            {
                Console.WriteLine("Please load valid format (.bmp or .dds) \n");
            }

            imgData.Close();
        }

        #region Load and validate BMP image data
        /// <summary>
        /// Populates BMP header and file details along with uncompressed image data
        /// </summary>
        /// <returns>True if data loaded successfully else False</returns>
        private bool LoadBMPData()
        {
            // Buffer to read BMP header and file details
            byte[] Buffer = new byte[54];
            imgData.Read(Buffer, 0, 54);

            //Populate file header details
            bmpFileHeader.bfType        = Helper.ByteToString(Buffer, 0, 2);
            bmpFileHeader.bfSize        = BitConverter.ToUInt32(Buffer, 2);
            bmpFileHeader.bfReserved1   = BitConverter.ToUInt16(Buffer, 6);
            bmpFileHeader.bfOffBits     = BitConverter.ToUInt32(Buffer, 10);

            //Populate info header details
            bmpInfoHeader.biSize            = BitConverter.ToUInt32(Buffer, 14);
            bmpInfoHeader.biWidth           = BitConverter.ToInt32(Buffer, 18);
            bmpInfoHeader.biHeight          = BitConverter.ToInt32(Buffer, 22);
            bmpInfoHeader.biPlanes          = BitConverter.ToUInt16(Buffer, 26);
            bmpInfoHeader.biBitCount        = BitConverter.ToUInt16(Buffer, 28);
            bmpInfoHeader.biCompression     = BitConverter.ToUInt32(Buffer, 30);
            bmpInfoHeader.biSizeImage       = BitConverter.ToUInt32(Buffer, 34);
            bmpInfoHeader.biXPelsPerMeter   = BitConverter.ToInt32(Buffer, 38);
            bmpInfoHeader.biYPelsPerMeter   = BitConverter.ToInt32(Buffer, 42);
            bmpInfoHeader.biClrUsed         = BitConverter.ToUInt32(Buffer, 46);
            bmpInfoHeader.biClrImportant    = BitConverter.ToUInt32(Buffer, 50);

            //Validate BMP image
            if (!ValidateBMPImage())
                return false;

            uint imageSize = bmpInfoHeader.biSizeImage;
            
            //If size of image data in header is zero then calculate image data size by subracting bfSize by bfOffBits
            if (imageSize == 0)
            {
                imageSize                   = bmpFileHeader.bfSize - bmpFileHeader.bfOffBits;
                bmpInfoHeader.biSizeImage   = imageSize;
            }

            // BMP pixel data to store the loaded image data
            bmpPixelData = new byte[imageSize];
            
            //Populate bmpPixelData with the image data
            imgData.Read(bmpPixelData, 0, (int)imageSize);
            return true;
        }

        /// <summary>
        /// Validates whether the image satisfies all the requirements to be an valid BMP image
        /// </summary>
        /// <returns>True or False</returns>
        private bool ValidateBMPImage()
        {
            var isValid = true;

            // Check if width of image is divisible by 4
            if ((bmpInfoHeader.biWidth % 4) != 0)
            {
                Console.WriteLine("Image width is not divisible by 4\n");
                isValid = false;
            }
            // Check if height of image is divisible by 4
            if ((bmpInfoHeader.biHeight % 4) != 0)
            {
                Console.WriteLine("Image height is not divisible by 4\n");
                isValid = false;
            }
            // Check if the image is of valid BMP format
            if (bmpFileHeader.bfType != "BM")
            {
                Console.WriteLine("Provided file is not a valid BMP image\n");
                isValid = false;
            }
            // Check if the BMP image has 24bits per pixel
            if (bmpInfoHeader.biBitCount != 24)
            {
                Console.WriteLine("Provided image doesn't have 24bits per pixel\n");
                isValid = false;
            }
            
            return isValid;

        }
        #endregion

        #region Load and validate DDS image data
        /// <summary>
        /// Populates DDS header details and DXT1 compressed image data from the filestream 
        /// </summary>
        /// <returns>True if data loaded successfully else False</returns>
        private bool LoadDDSData()
        {
            // Buffer to read DDS header details
            byte[] dataBuffer = new byte[128];
            imgData.Read(dataBuffer, 0, 128);
           
            //Populate DDS Header
            ddsHeader.dwSize                = BitConverter.ToUInt32(dataBuffer, 4);
            ddsHeader.dwFlags               = BitConverter.ToUInt32(dataBuffer, 8);
            ddsHeader.dwHeight              = BitConverter.ToUInt32(dataBuffer, 12);
            ddsHeader.dwWidth               = BitConverter.ToUInt32(dataBuffer, 16);
            ddsHeader.dwPitchOrLinearSize   = BitConverter.ToUInt32(dataBuffer, 20);
            ddsHeader.dwDepth               = BitConverter.ToUInt32(dataBuffer, 24);
            ddsHeader.dwMipMapCount         = BitConverter.ToUInt32(dataBuffer, 28);
            var reservIndex = 0;
            ddsHeader.dwReserved1 = new uint[11];
            for (int count = 28; count < 72; count += 4)
            {
                ddsHeader.dwReserved1[reservIndex] = BitConverter.ToUInt32(dataBuffer, count);
                reservIndex++;
            }
            ddsHeader.ddspf.dwsize          = BitConverter.ToUInt32(dataBuffer, 76);
            ddsHeader.ddspf.dwflags         = BitConverter.ToUInt32(dataBuffer, 80);
            ddsHeader.ddspf.dwfourCC        = BitConverter.ToUInt32(dataBuffer, 84);
            ddsHeader.ddspf.dwRGBBitCount   = BitConverter.ToUInt32(dataBuffer, 88);
            ddsHeader.ddspf.dwRBitMask      = BitConverter.ToUInt32(dataBuffer, 92);
            ddsHeader.ddspf.dwGBitMask      = BitConverter.ToUInt32(dataBuffer, 96);
            ddsHeader.ddspf.dwBBitMask      = BitConverter.ToUInt32(dataBuffer, 100);
            ddsHeader.ddspf.dwABitMask      = BitConverter.ToUInt32(dataBuffer, 104);
            ddsHeader.dwCaps                = BitConverter.ToUInt32(dataBuffer, 108);
            ddsHeader.dwCaps2               = BitConverter.ToUInt32(dataBuffer, 112);
            ddsHeader.dwCaps3               = BitConverter.ToUInt32(dataBuffer, 116);
            ddsHeader.dwCaps4               = BitConverter.ToUInt32(dataBuffer, 120);
            ddsHeader.dwReserved2           = BitConverter.ToUInt32(dataBuffer, 124);


            if (!ValidateDDSImage(Helper.ByteToString(dataBuffer, 0, 4)))
                return false;

            //Calculate main image size, (blocksize = 8 and packing = 4 for DXT1)
            uint mainImageSize = Math.Max((uint)1, (ddsHeader.dwWidth + 3) / 4) * Math.Max((uint)1, (ddsHeader.dwHeight + 3) / 4) * 8;
            
            //Read the main data
            ddsPixelData = new byte[mainImageSize];
            imgData.Read(ddsPixelData, 0, (int)mainImageSize);
            return true;
        }

        /// <summary>
        /// Validates whether the image satisfies all the requirements to be an valid DDS image
        /// </summary>
        /// <param name="imageFormat">Image format from the first line of the file</param>
        /// <returns>True or False</returns>
        private bool ValidateDDSImage(string imageFormat)
        {
            var isValid = true;
            // Check if width of image is divisible by 4
            if ((ddsHeader.dwWidth % 4) != 0)
            {
                Console.WriteLine("Image width is not divisible by 4\n");
                isValid = false;
            }
            // Check if height of image is divisible by 4
            if ((ddsHeader.dwHeight % 4) != 0)
            {
                Console.WriteLine("Image height is not divisible by 4\n");
                isValid = false;
            }
            // Check if image is of valid DDS format
            if (imageFormat != "DDS ")
            {
                Console.WriteLine("Provided file is not a valid DDS image\n");
                isValid = false;
            }
            //Check if image has valid structure sizes
            if (ddsHeader.dwSize != 124 || ddsHeader.ddspf.dwsize != 32)
            { 
                Console.WriteLine("Image structure size is wrong\n");
                isValid = false;
            }
            //Check if image contains compressed RGB image data
            if (ddsHeader.ddspf.dwflags != DDSFormat.ddpf)
            { 
                Console.WriteLine("Image does not have RGB compressed data\n");
                isValid = false;
            }
            //Check if image is compressed with DXT1 or not
            if (ddsHeader.ddspf.dwfourCC != DDSFormat.dxt1)
            { 
                Console.WriteLine("Image is not DXT1 compressed\n");
                isValid = false;
            }

            return isValid;
        }
        #endregion
        
    }
}
