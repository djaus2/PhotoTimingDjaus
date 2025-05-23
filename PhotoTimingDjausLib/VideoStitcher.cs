﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;

namespace PhotoTimingDjaus
{
    public class VideoStitcher
    {
        private string videoPath;
        private string outputPath;
        private int startTimeSeconds;
        private int Fps = 30;
        const int OneMinute = 60;

        // Constructor to initialize the video stitcher parameters
        public VideoStitcher(string videoPath, string outputPath, int startTimeSeconds)
        {
            this.videoPath = videoPath;
            this.outputPath = outputPath;
            this.startTimeSeconds = startTimeSeconds - (startTimeSeconds % 10);

        }

        // Main method to start the stitching process
        public int Stitch()
        {
            // Delete the file if it already exists
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                Console.WriteLine($"Existing file at '{outputPath}' has been deleted.");
            }

            // Open the video file
            using var capture = new VideoCapture(videoPath);

            Fps = (int)capture.Fps;

            if (!capture.IsOpened())
            {
                Console.WriteLine("Failed to open the video file.");
                return 0;
            }

            // Check if the start time exceeds the video duration
            double videoDurationSeconds = capture.FrameCount / capture.Fps; // Calculate total video duration in seconds
            if (startTimeSeconds > videoDurationSeconds)
            {
                Console.WriteLine($"Error: Start time ({startTimeSeconds} seconds) exceeds video duration ({videoDurationSeconds:F2} seconds).");
                return 0;
            }

            // Calculate the frame to start stitching
            int startFrame = startTimeSeconds * (int)capture.Fps;

            // Move to the starting frame
            capture.Set(VideoCaptureProperties.PosFrames, startFrame);

            // List to store the extracted vertical lines
            var verticalLines = new List<Mat>();

            // Process each frame starting from the specified time
            Mat frame = new Mat();
            while (capture.Read(frame))
            {
                if (frame.Empty())
                    break;

                // Get the middle column of the frame
                int middleColumn = frame.Cols / 2;
                var middleColumnMat = frame.Col(middleColumn); // Extract middle column

                // Add the column to the list
                verticalLines.Add(middleColumnMat.Clone()); // Clone to ensure memory safety
            }

            // Determine height and width for the stitched image
            int stitchedHeight = verticalLines[0].Rows; // All columns have the same height
            int stitchedWidth = verticalLines.Count;    // Number of columns

            // Create an empty Mat for the stitched image
            var stitchedImage = new Mat(stitchedHeight + 100, stitchedWidth, MatType.CV_8UC3, new Scalar(0, 0, 0)); // Extra 100 pixels for markers

            // Populate the stitched image with vertical lines
            for (int i = 0; i < verticalLines.Count; i++)
            {
                var column = verticalLines[i];
                column.CopyTo(stitchedImage.RowRange(0, stitchedHeight).Col(i)); // Copy column to the corresponding position

                int currentTimeSeconds = startTimeSeconds + i / Fps; // Current time (relative to start)
                int currentMinute = currentTimeSeconds / OneMinute; // Current minute
                int currentSecond = currentTimeSeconds % OneMinute; // Seconds within the current minute

                // Add markers
                if (currentSecond == 0 && i % Fps == 0) // Red minute marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + 100), new Scalar(0, 0, 255), 3); // Red line
                    if (i == 0) // First marker (special alignment)
                    {
                        int firstMinuteLabel = (startTimeSeconds % OneMinute == 0) ? currentMinute : currentMinute - 1; // Adjust label only if start time is not an exact minute
                        Cv2.PutText(stitchedImage, $"{firstMinuteLabel} min", new Point(i, stitchedHeight + 90), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Align text to the left with ' min'
                    }
                    else
                    {
                        Cv2.PutText(stitchedImage, $"{currentMinute} min", new Point(i + 5, stitchedHeight + 90), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Normal alignment with ' min'
                    }
                }
                else if (currentSecond % 10 == 0 && i % Fps == 0) // Green 10-second marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + 50), new Scalar(0, 255, 0), 2); // Green line
                    if (i == 0) // First marker is a 10-second marker
                    {
                        int firstMinuteLabel = currentMinute; // Do not decrement for a 10-second marker
                        Cv2.PutText(stitchedImage, $"{currentSecond}", new Point(i, stitchedHeight + OneMinute), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Align text to the left
                        Cv2.PutText(stitchedImage, $"{firstMinuteLabel} min", new Point(i, stitchedHeight + 90), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Add minute number aligned below with ' min'
                    }
                    else
                    {
                        Cv2.PutText(stitchedImage, $"{currentSecond}", new Point(i - 10, stitchedHeight + OneMinute), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Normal alignment
                    }
                }
                else if (currentSecond % 5 == 0 && i % Fps == 0) // Yellow 5-second marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + 25), new Scalar(0, 255, 255), 1); // Yellow line
                }
                else if (i % Fps == 0) // Blue 1-second marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + 12), new Scalar(255, 255, 255), 1); // White line
                }
            }

            // Save the stitched image
            Cv2.ImWrite(outputPath, stitchedImage);

            Console.WriteLine($"Stitched image with markers saved at '{outputPath}'.");
            return (int) videoDurationSeconds;
        }

        private void StitchUpWorker(string videoPath, string outputPath, int startTimeSeconds, Action<IAsyncResult> callback)
        {
            int videoLength = 0;

            if (!File.Exists(videoPath))
            {
                File.Delete(outputPath);
            }


            // Show the busy indicator
            //BusyIndicator.Visibility = Visibility.Visible;
            //FinishTime.Visibility = Visibility.Hidden;
            //FinishTimeLabel.Visibility = FinishTime.Visibility;

            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, args) =>
            {
                // Call the stitching process
                var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoPath, outputPath, startTimeSeconds);
                videoLength = videoStitcher.Stitch();
            };

            worker.RunWorkerCompleted += (s, args) =>
            {

                // Hide the busy indicator
                //BusyIndicator.Visibility = Visibility.Collapsed;

                // Display the stitched image
                if (File.Exists(outputPath))
                {
                    //null, false, null)
                    var argsx = new AsyncCompletedEventArgs(null, false, videoLength);
                    callback.Invoke((IAsyncResult)argsx);
                }
                else
                {
                    
                    //MessageBox.Show("Failed to create the stitched image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                //StitchButton.Visibility = Visibility.Visible; // Hide the button
                //StitchButton.Width = 200;
                //StitchButton.IsEnabled = true; // Re-enable the button
            };

            worker.RunWorkerAsync();
        }

    }
}