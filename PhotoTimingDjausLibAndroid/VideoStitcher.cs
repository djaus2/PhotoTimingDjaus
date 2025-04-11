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

namespace PhotoTimingDjaus
{
    public class VideoStitcher
    {
        private string videoPath;
        private string outputFilename;
        private int startTimeSeconds;
        private int Fps = 30;
        const int OneMinute = 60;

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
            //Mat stitchedImage = new Mat();
            // Initialize the stitched image with the correct dimensions
            int frameHeight = (int)capture.Get(CapProp.FrameHeight);
            Mat stitchedImage = new Mat(frameHeight, 0, DepthType.Cv8U, 3); // Start with 0 columns
            int framesCount = 0;
            while (true)
            {
                if (framesCount == 1000)
                    break;
                Mat frame = new Mat();
                capture.Read(frame);

                if (frame.IsEmpty)
                    break;
                framesCount++;
                System.Diagnostics.Debug.WriteLine($"Frames: {framesCount}");
                // Extract the middle vertical line
                int middleColumn = frame.Cols / 2;
                //Mat middleLine = frame.ColRange(middleColumn, middleColumn + 1);
                using (Mat middleLine = frame.Col(middleColumn))
                {

                    // Append the middle line to the stitched image
                    if (stitchedImage.IsEmpty)
                    {
                        stitchedImage = middleLine.Clone();
                    }
                    else
                    {
                        CvInvoke.HConcat(new Mat[] { stitchedImage, middleLine }, stitchedImage);
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