using OpenCvSharp;
using PhotoTimingDjausLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PhotoTimingDjaus
{
    public class VideoStitcher
    {
        private string videoFilePath;
        private string outputFilePath;
        private string guninfoFilePath = @"C:\temp\vid\guninfo.txt";
        private int startTimeSeconds;
        private int Fps = 30;

        public double videoLength = 0; // Length of the video in seconds
        public double GunTime { get; set; } = 5.00;
        
        
        int axisHeight = 100;  //Vertical height of time axis at bottom below stitched image
        int audioHeight = 100; // Vertical height of space for audio volume graph below axis.
        const int OneMinute = 60;

        // Constructor to initialize the video stitcher parameters
        public VideoStitcher(string videoPath, string outputPath, string _guninfoPath, int startTimeSeconds = 0, int _axisHeight = 100, int _audioHeight = 100)
        {
            this.videoFilePath = videoPath;
            this.outputFilePath = outputPath;
            this.startTimeSeconds = startTimeSeconds - (startTimeSeconds % 10);
            this.audioHeight = _audioHeight;
            this.axisHeight = _axisHeight;
            this.guninfoFilePath = _guninfoPath;
        }

        // Main method to start the stitching process
        public double Stitch()
        {
            if(!File.Exists(guninfoFilePath))
            {
                Console.WriteLine("Gun info file not found. Please ensure 'guninfo.txt' exists in 'C:\\temp\\vid\\'.");
                return 0;
            }
            var data = File.ReadAllLines(guninfoFilePath)
                .Skip(1)
               .Select(line => double.Parse(line.Split(',')[3])) // Extract RMS loudness
               .ToArray();

            var min = data.Min();

            var max = data.Max();

            var range = max - min;

            int Amp = 10;

            // Normalize the data to a range of 0-Amp
            var normalizedData = data.Select(x => (double)(Amp*(x - min) / range)).ToArray();
            var expData = normalizedData.Select(x => Math.Round(Math.Pow(x,10),0)).ToArray(); // Exponential scaling for better visibility
            var maxx = expData.Max();
            expData = expData.Select(x => audioHeight*x/maxx).ToArray(); // Ensure no negative values

            var average = Math.Round(expData
            .OrderByDescending(n => n) // Sort in descending order
            .Skip(20)                 // Skip the top 20 values
            .Average(),0);
            double threshold = expData.Max() / (audioHeight*10);

            // Find the index of the first value greater than or equal to the threshold
            int index = Array.FindIndex(expData, x => x >= threshold);
            
            int count = expData.Take(index+1).Count(x => x >= expData[index]);
            double r = expData[index - 1] / (expData.Max());
            Console.WriteLine($"Count: {count}");
            // Delete the file if it already exists
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
                Console.WriteLine($"Existing file at '{outputFilePath}' has been deleted.");
            }

            // Open the video file
            using var capture = new VideoCapture(videoFilePath);

            Fps = (int)Math.Round(capture.Fps);
            //Fps = 30;

            if (!capture.IsOpened())
            {
                Console.WriteLine("Failed to open the video file.");
                return 0;
            }

            // Check if the start time exceeds the video duration
            double videoDurationSeconds = capture.FrameCount / capture.Fps; // Calculate total video duration in seconds
            //Audio frames are different to video frames
            double ratio = (double)FFMpegActions.numAudioFrames / capture.FrameCount;

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
            var stitchedImage = new Mat(stitchedHeight + 1 + audioHeight + axisHeight, stitchedWidth, MatType.CV_8UC3, new Scalar(0,0,0)); // Extra 2 x 100 pixels for markers and audio graph

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
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + axisHeight), new Scalar(0, 0, 255), 3); // Red line
                    if (i == 0) // First marker (special alignment)
                    {
                        int firstMinuteLabel = (startTimeSeconds % OneMinute == 0) ? currentMinute : currentMinute - 1; // Adjust label only if start time is not an exact minute
                        Cv2.PutText(stitchedImage, $"{firstMinuteLabel} min", new Point(i, stitchedHeight + (int)(0.9 *axisHeight)), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Align text to the left with ' min'
                    }
                    else
                    {
                        Cv2.PutText(stitchedImage, $"{currentMinute} min", new Point(i + 5, stitchedHeight + (int)(0.9 * axisHeight)), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Normal alignment with ' min'
                    }
                }
                else if (currentSecond % 10 == 0 && i % Fps == 0) // Green 10-second marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + (int)(0.5 * axisHeight)), new Scalar(0, 255, 0), 2); // Green line
                    if (i == 0) // First marker is a 10-second marker
                    {
                        int firstMinuteLabel = currentMinute; // Do not decrement for a 10-second marker
                        Cv2.PutText(stitchedImage, $"{currentSecond}", new Point(i, stitchedHeight + OneMinute), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Align text to the left
                        Cv2.PutText(stitchedImage, $"{firstMinuteLabel} min", new Point(i, stitchedHeight + (int)(0.9 * axisHeight)), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Add minute number aligned below with ' min'
                    }
                    else
                    {
                        Cv2.PutText(stitchedImage, $"{currentSecond}", new Point(i - 10, stitchedHeight + (int)(0.7 * axisHeight)), HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1); // Normal alignment
                    }
                }
                else if (currentSecond % 5 == 0 && i % Fps == 0) // Yellow 5-second marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + (int)(0.25 * axisHeight)), new Scalar(0, 255, 255), 1); // Yellow line
                }
                else if (i % Fps == 0) // Blue 1-second marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + (int)(0.12 * axisHeight)), new Scalar(255, 255, 255), 1); // White line
                }
                System.Diagnostics.Debug.WriteLine($"{i} {expData[i]}");
                int audioframe = (int)Math.Round(i * ratio);
                int i2 = i;
                if (i!=0)
                {
                    i2 = i - 1;

                }
                int audioframe2 = (int)Math.Round(i2 * ratio); 
                Cv2.Line(stitchedImage, new Point(i2, stitchedHeight + axisHeight + audioHeight - expData[audioframe2]), new Point(i, stitchedHeight + axisHeight + audioHeight - expData[audioframe]), new Scalar(0, 255, 255), 1); // Read line
            }
            int indexx = (int)Math.Round(index / ratio);
            GunTime = (double)indexx / Fps;

            Cv2.Line(stitchedImage, new Point(indexx, 0), new Point(indexx, stitchedHeight), new Scalar(255, 255, 255), 1); // White line

            // Save the stitched image.
            Cv2.ImWrite(outputFilePath, stitchedImage);

            Console.WriteLine($"Stitched image with markers saved at '{outputFilePath}'.");
            return videoDurationSeconds;
        }

        private void StitchUpWorker(string videoPath, string outputPath, int startTimeSeconds, Action<IAsyncResult> callback)
        {

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
                var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoPath, outputPath,"", startTimeSeconds);
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