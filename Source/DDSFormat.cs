/**
 * Author:  Rakesh Kumar Vali
 * Created: 24.09.2018
 * Summary: DDS file format details
 **/

namespace BMP2DDS.Source
{
    class DDSFormat
    {
        
        public static uint dxt1         = 0x31545844;	//Hex value of DXT1 for dwfourCC
        public static uint ddsFlags     = 0x81007;      //DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_LINEARSIZE
        public static uint ddpf         = 0x4;			// Compressed RGB data flag
        public static uint ddsTexture   = 0x1000;       //Required in DDS header

        //Helper values used in compression
        public static uint noOfRGBBytes             = 3;
        public static uint bytesInCompressedBlock   = 8;
        public static uint pixelsInBlock            = 16;
        public static uint pixelsInBlockRow         = 4;
    }

    public struct DDSPixelFormat
    {
        public uint dwsize;             //Structure size
        public uint dwflags;            //Values which indicate what type of data is in the surface
        public uint dwfourCC;           //Four-character codes for specifying compressed or custom formats
        public uint dwRGBBitCount;      //Number of bits in an RGB (possibly including alpha) format
        public uint dwRBitMask;         //Red (or lumiannce or Y) mask for reading color data
        public uint dwGBitMask;         //Green (or U) mask for reading color data
        public uint dwBBitMask;         //Blue (or V) mask for reading color data
        public uint dwABitMask;         //Alpha mask for reading alpha data
    }
    public struct DDSHeader
    {
        public uint     dwSize;                 //Size of structure
        public uint     dwFlags;                //Flags to indicate which members contain valid data
        public uint     dwHeight;               //Surface height (in pixels)
        public uint     dwWidth;                //Surface width (in pixels)
        public uint     dwPitchOrLinearSize;    //The pitch or number of bytes per scan line in an uncompressed texture
        public uint     dwDepth;                //Depth of a volume texture (in pixels), otherwise unused
        public uint     dwMipMapCount;          //Number of mipmap levels, otherwise unused
        public uint[]   dwReserved1;            //Unused
        public DDSPixelFormat ddspf;            //The pixel format
        public uint     dwCaps;                 //Specifies the complexity of the surfaces stored
        public uint     dwCaps2;                //Additional detail about the surfaces stored
        public uint     dwCaps3;                //Unused
        public uint     dwCaps4;                //Unused
        public uint     dwReserved2;			//Unused
    }
}
