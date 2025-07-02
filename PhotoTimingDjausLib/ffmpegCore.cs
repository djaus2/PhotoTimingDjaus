using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using FFMpegCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;

namespace PhotoTimingDjausLib

{
    public static class PngMetadataHelper
    {

        public static string AppendGunTimeImageFilename(string imagePath, double gunTime)
        {

            if (!System.IO.File.Exists(imagePath))
            {
                throw new System.IO.FileNotFoundException($"The specified video file does not exist: {imagePath}");
            }
            if (imagePath.Contains("_Start_", StringComparison.OrdinalIgnoreCase))
            {
                imagePath = imagePath.Substring(0, imagePath.IndexOf("_Start_", StringComparison.OrdinalIgnoreCase));
                imagePath = $"{imagePath}.png";
            }
            string gunTimeString = gunTime.ToString("F3");

            //gunTimeString = gunTimeString.Replace(":", "--");
            string extension = imagePath.Substring(imagePath.Length - 4, 4); // Get the last 4 characters for extension check
            if ((string.IsNullOrEmpty(extension)) ||
                (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The input file must be an PNG image file.");
            }
            string outputPath = imagePath.Replace(".png", $"_Start_{gunTimeString}_.png", StringComparison.OrdinalIgnoreCase);

            System.IO.File.Copy(imagePath, outputPath, true);
            return outputPath;

        }
        public static void RetitleImage(string imageFilePath, string title)
        {
            // Load the image
#pragma warning disable CA1416 // Validate platform compatibility
            using (Image image = Image.FromFile(imageFilePath))
            {
                // Create a property item (metadata) using a safer approach
                //PropertyItem propItem = CreatePropertyItem();
                PropertyItem propItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));

                propItem.Id = 0x0320; // Property ID for Title
                propItem.Type = 2;    // ASCII type
                propItem.Value = Encoding.ASCII.GetBytes(title + '\0');
                propItem.Len = title.Length + 1;

                // Add the property to the image
                image.SetPropertyItem(propItem);

                // Save the image
                image.Save("output.png", ImageFormat.Png);
            }
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public static string? GetPngTitle(string filePath)
    {
#pragma warning disable CA1416 // Validate platform compatibility
            using (var image = Image.FromFile(filePath))
        {
            // PropertyItem ID for "Image Title" is 0x0320
            const int TitlePropertyId = 0x0320;

            try
            {
                var propItem = image.GetPropertyItem(TitlePropertyId);
                    if(propItem == null)
                    {
                        return "Property is not set.";
                    }
                    if (propItem.Type != 2) // Ensure it's ASCII type
                    {
                        return "Property is not of type ASCII.";
                    }
                    else
                        return Encoding.ASCII.GetString(propItem.Value).TrimEnd('\0');
            }
            catch (ArgumentException)
            {
                // Property doesn't exist
                return null;
            }
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }


        private static PropertyItem CreatePropertyItem()
        {
            // Create a new PropertyItem using a safer approach
            using (var tempImage = new Bitmap(1, 1))
            {
                return tempImage.PropertyItems.First();
            }
        }
    }
}

