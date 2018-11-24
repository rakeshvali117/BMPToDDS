/**
 * Author:  Rakesh Kumar Vali
 * Created: 24.09.2018
 * Summary: This application converts BMP(24bit per pixel uncompressed) image to DDS (DXT1 compressed) image. 
 **/
using System;

namespace BMP2DDS.Source
{
    class Program
    {
        static void Main(string[] args)
        {
            bool result = true;
            Console.WriteLine("Image Converter - BMP (24bit per pixel uncompressed data) to DDS(DXT1 BlockCompression(BC1) compressed) and DDS to BMP\n\n" +
                "Command to open file: <FilePath>(Please provide file extention)\n\n" +
                "Command to exit the application: 'exit'\n");

            while (result)
            {
                Console.Write("Enter Command: ");
                string command = Console.ReadLine();

                // Exit application if the command is 'exit'
                if (command.ToLower() == "exit")
                {
                    result = false;
                    continue;
                }

                // Instantiate ImageLoader object to begin the process
                ImageLoader imgLoader = new ImageLoader(command);
                imgLoader.LoadImageFromPath();
            }
        }
    }
}
