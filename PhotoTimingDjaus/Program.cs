using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO; // Required for File operations

namespace PhotoTimingDjaus
{
    class Program
    {
        static void Main(string[] args)
        {
            // Path to the video file
            string videoPath = @"C:\Users\me\Documents\athletic.mp4";

            // Path to save the stitched image
            string outputPath = @"c:\temp\vid\stitched_image.png";

            int startTimeSeconds = 60; // Start time in seconds

            VideoStitcher video = new VideoStitcher(videoPath, outputPath, startTimeSeconds);
            video.Stitch();

        }
    }
}