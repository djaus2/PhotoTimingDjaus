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
using FFMpegCore.Builders.MetaData;

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

        public static async Task<bool> AddMetadataToPng(string inputPath, string outputPath, string comment = null, string title = null)
        {
            if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Input and output paths must be provided.");

            var args = new List<string>();

            if (!string.IsNullOrEmpty(title))
                args.Add($"-metadata title=\"{title}\"");

            if (!string.IsNullOrEmpty(comment))
                args.Add($"-metadata comment=\"{comment}\"");

            string customArgs = string.Join(" ", args);
            customArgs = "-c copy -map_metadata 0 " + customArgs;

            try
            {
                string tempPath = @"C:\Users\david\OneDrive\Documents\Downloads\ffmpeg-master-latest-win64-gpl-shared\ffmpeg-master-latest-win64-gpl-shared\bin";
                string? currentPath = Environment.GetEnvironmentVariable("PATH");

                if (!string.IsNullOrEmpty(currentPath))
                {
                    if (!currentPath.Split(';').Contains(tempPath))
                    {
                        Environment.SetEnvironmentVariable("PATH", currentPath + ";" + tempPath);
                    }

                    await FFMpegArguments
                        .FromFileInput(inputPath)
                        .OutputToFile(outputPath, overwrite: true, options => options
                            .WithCustomArgument(customArgs))
                        .ProcessAsynchronously();
                }

                return true;
            }
            catch (Exception ex)
            {
                // You can log or handle specific exceptions here
                Console.WriteLine($"Metadata write failed: {ex.Message}");
                return false;
            }
        }

        public static async  Task<Tuple<string,string>?> GetMetaInfo(string filePath)
        {
            string tempPath = @"C:\temp\vid\exiftool-13.32_64\exiftool-13.32_64";//Need to download from https://exiftool.org/
            if (!File.Exists(Path.Combine(tempPath, "exiftool(-k).exe")))
            {
                Console.WriteLine("ExifTool not found. Ensure it's extracted to the specified path.");
                return null;
            }
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"{filePath} not found.");
                return null;
            }

            string? currentPath = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrEmpty(currentPath))
            {
                if (!currentPath.Split(';').Contains(tempPath))
                {
                    Environment.SetEnvironmentVariable("PATH", currentPath + ";" + tempPath);
                }

                //Need Nuget package: dotnet add package SharpExifTool
                using var exiftool = new SharpExifTool.ExifTool();

                // Read metadata
                var metadata = await exiftool.ExtractAllMetadataAsync(filePath);

                string title = metadata.FirstOrDefault(kvp => kvp.Key.Contains("Title", StringComparison.OrdinalIgnoreCase)).Value;
                string comment = metadata.FirstOrDefault(kvp => kvp.Key.Contains("Comment", StringComparison.OrdinalIgnoreCase)).Value;
                if (comment == null)
                    comment = "";
                if (string.IsNullOrEmpty(title))
                {
                    Console.WriteLine("No metadata found.");
                    return null;
                }

                return new Tuple<string, string>(title, comment);
            }
            return null;

        }

        public static async Task<bool> SetMetaInfo(string filePath, string title, string meta)
        {
            string tempPath = @"C:\temp\vid\exiftool-13.32_64\exiftool-13.32_64";//Need to download from https://exiftool.org/
            if (!File.Exists(Path.Combine(tempPath, "exiftool(-k).exe")))
            {
                Console.WriteLine("ExifTool not found. Ensure it's extracted to the specified path.");
                return false;
            }
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"{filePath} not found.");
                return false;
            }

            string? currentPath = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrEmpty(currentPath))
            {
                if (!currentPath.Split(';').Contains(tempPath))
                {
                    Environment.SetEnvironmentVariable("PATH", currentPath + ";" + tempPath);
                }

                //Need Nuget package: dotnet add package SharpExifTool
                using var exiftool = new SharpExifTool.ExifTool();


                // Write metadata
                await exiftool.WriteTagsAsync(filePath, new Dictionary<string, string>
                {
                    ["Title"] = title,
                    ["Comment"] = meta
                },true);

                bool done = false;
                do
                {
                    var metadata = await exiftool.ExtractAllMetadataAsync(filePath);
                    if (metadata == null)
                    {
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        if (metadata.Count() == 0)
                        {
                            Thread.Sleep(1000);
                        }
                        else
                        {
                            done = true;
                        }
                    }
                } while(!done);
            }
            return true;

        }
    }

}

