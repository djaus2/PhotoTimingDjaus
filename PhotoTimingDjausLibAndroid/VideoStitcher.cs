//using OpenCvSharp;
using Emgu.CV;
using Emgu.CV.Structure;
using Android.OS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using Emgu.CV.CvEnum;
using System.Drawing;
using Android.Icu.Number;

namespace PhotoTimingDjaus
{
    public class VideoStitcher
    {
        private string videoPath;
        private string outputFilename;
        private int startTimeSeconds;
        private int Fps = 30;
        const int OneMinute = 60;
        public bool cancel { get; set; } = false;

        public string outputFilepath { get; set; } = "";

        // Constructor to initialize the video stitcher parameters
        public VideoStitcher(string videoPath, string outputPath, int startTimeSeconds)
        {
            this.videoPath = videoPath;
            this.outputFilename = outputPath;
            this.startTimeSeconds = startTimeSeconds - (startTimeSeconds % 10);

        }

        // Main method to start the stitching process
        public int Stitch()
        {


            VideoCapture capture = new VideoCapture(videoPath);
            var height = capture.QueryFrame().Height;
            var width = capture.QueryFrame().Width;
            var depth = capture.QueryFrame().Depth;
            var channels = capture.QueryFrame().NumberOfChannels;
            //Mat stitchedImage = new Mat();
            // Initialize the stitched image with the correct dimensions
            int frameHeight = (int)capture.Get(CapProp.FrameHeight);
            Mat stitchedImage = new Mat(frameHeight, 0, depth, channels); // Start with 0 columns
            int framesCount = 0;
            cancel = false;
            while (!cancel)
            {
                if (framesCount == 1000)
                    break;
                // Read next frame
                int i = framesCount;
                Mat frame = new Mat(height, width, depth, channels);
                //Mat frame = new Mat();
                capture.Read(frame);
               
                if (frame.IsEmpty)
                    break;
                framesCount++;

                System.Diagnostics.Debug.WriteLine($"\t\t\t\t=====Frame====: {framesCount}");
                // Extract the middle vertical line
                int middleColumn = frame.Cols / 2;
                //Mat middleLine = frame.ColRange(middleColumn, middleColumn + 1);
                using (Mat middleLine = frame.Col(middleColumn))
                {
                    //Ref: https://www.emgu.com/wiki/index.php?title=Working_with_Images#Accessing_the_pixels_from_Mat
                    Image<Bgr, Byte> img = middleLine.ToImage<Bgr, Byte>();
                    int currentTimeSeconds = startTimeSeconds + i / Fps; // Current time (relative to start)
                    int currentMinute = currentTimeSeconds / OneMinute; // Current minute
                    int currentSecond = currentTimeSeconds % OneMinute; // Seconds within the current minute

                    // Add markers
                    Bgr bgr = new Bgr(0, 0, 0);
                    Bgr red = new Bgr(0, 0, 255);
                    Bgr black = new Bgr(255, 255, 255);
                    int ht = 100;
                    int tick = 0;
                    int startPix = img.Height - ht;
                    if ( currentSecond == 0 && i % Fps == 0) // Red minute marker
                    {
                        bgr = new Bgr(0, 0, 255); // Red color
                        tick = ht;
                    }
                    else if (currentSecond % 10 == 0 && i % Fps == 0) // Green 10-second marker
                    {
                        tick = (2 *ht )/ 3;
                        bgr = new Bgr(255,0, 0); // Blue color
                    }
                    else if (currentSecond % 5 == 0 && i % Fps == 0) // Yellow 5-second marker
                    {
                        tick =  ht / 2; 
                        bgr = new Bgr(0, 255, 0); // Green color
                    }
                    else if (i % Fps == 0) // Black 1-second marker
                    {
                        tick = ht / 3;
                        bgr = new Bgr(0, 0, 0); // Black color
                    }
                    tick = startPix + tick;
                    // Set bottom 20 pixels to black
                    
                    for (int ii = startPix; ii < img.Height; ii++)
                    {
                        if (ii == startPix)  //Mark tick 2 pixels wide
                            img[ii, 0] = red;
                        else if (ii < tick) 
                            img[ii, 0] = bgr;
                        else
                            img[ii, 0] = black; // Set to black
                    }
                    var middleLineMod = img.Mat;
                    // Append the middle line to the stitched image
                    if (stitchedImage.IsEmpty)
                    {
                        stitchedImage = middleLineMod.Clone();
                    }
                    else
                    {
                        CvInvoke.HConcat(new Mat[] { stitchedImage, middleLineMod }, stitchedImage);
                        //CvInvoke.VConcat(new Mat[] { stitchedImage, middleLine }, stitchedImage);

                    }
                }
            }
            // Get the path to the Pictures folder
            string? picturesFolderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures)?.AbsolutePath;

            if(picturesFolderPath==null)
            {
                throw new InvalidOperationException("Unable to retrieve the path to the Pictures directory. Ensure the external storage is accessible.");
            }
            // Define the full path for the stitched image as a PNG
            outputFilepath = System.IO.Path.Combine(picturesFolderPath, outputFilename);


            // Delete the file if it already exists
            if (File.Exists(outputFilepath))
            {
                File.Delete(outputFilepath);
                System.Diagnostics.Debug.WriteLine($"Existing file at '{outputFilepath}' has been deleted.");
            }

            // Save or display the stitched image
            CvInvoke.Imwrite(outputFilepath, stitchedImage);

            // Determine height and width for the stitched image
            int stitchedHeight = stitchedImage.Height; // All columns have the same height
            int stitchedWidth = stitchedImage.Width;    // Number of columns
            int videoDurationMilliSeconds = (int)(framesCount * 1000)/ Fps; // Calculate video duration in seconds
            return videoDurationMilliSeconds; // videoDurationSeconds;
        }

     }
}