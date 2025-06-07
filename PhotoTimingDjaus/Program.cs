using OpenCvSharp;
using DetectAudioFlash;
using System;
using System.Collections.Generic;
using System.IO; // Required for File operations

namespace PhotoTimingDjaus
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Path to the video file
            string videoPath = @"C:\temp\vid\whistle.mp4";

            // Path to save the stitched image
            string outputPath = @"c:\temp\vid\stitched_image.png";

            // Path to the video file
            string videoPath1 = @"c:\temp\vid\whistle.mp4";

            // Path to save the stitched image
            string outputPath2 = @"c:\temp\vid\truncated.mp4";

            string audioPath = @"c:\temp\vid\audio.wav";

            int startTimeSeconds = 60; // Start time in seconds

            //var gun = FFMpegActions.GetGunTimeofStart(videoPath1, audioPath, outputPath2);

            //VideoStitcher video = new VideoStitcher(videoPath, outputPath, startTimeSeconds);
            //video.Stitch();
            string guninfoTempPath = @"C:\temp\vid\guninfoTemp.txt";
            string guninfoPath = @"C:\temp\vid\guninfo.txt";
            FFMpegActions.Filterdata(videoPath, guninfoTempPath, guninfoPath);
        }
    }
}