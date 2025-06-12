using OpenCvSharp;
using DetectAudioFlash;
using DetectVideoFlash;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;
using PhotoTimingDjaus.Enums; // Ensure this namespace is correct for TimeFromMode
using OpenCvSharp.Extensions;


namespace PhotoTimingDjaus
{
    public class VideoStitcher
    {
        private string videoFilePath;
        private string outputFilePath;

        // Following are determined in GetGunTimenFrameIndex():
        ///////////////////////////////////////////////////////////////////
        public double videoDuration = 0;
        public int videoFrameCount = 0;
        private double selectedStartTime;
        public double GunTime { get; set; } = 0.0;
        public int GunTimeIndex = 0;
        public int Fps = 30;
        private double[] audioData = new double[0];
        private double ratio = 1;  //The ratio of audio frames to video frames, used for stitching audio data with video frames
        ///////////////////////////////////////////////////////////////////
        private TimeFromMode timeFromMode;
        
        private int threshold = 1000; // max volume/Threshold for gun detection in audio, default is 1000      
        private Scalar GunTimeColor = new Scalar(255, 255, 255, 1); // White line
        int axisHeight = 100;  //Vertical height of time axis at bottom below stitched image
        int audioHeight = 100; // Vertical height of space for audio volume graph below axis.
        const int OneMinute = 60;

        // Constructor to initialize the video stitcher parameters
        public VideoStitcher(string videoPath,  Scalar _gunTimeColor, string outputPath, double _selectedStartTime = 0, int _axisHeight = 100, int _audioHeight = 100, TimeFromMode _timeFromMode = TimeFromMode.FromButtonPress, int _threshold=1000)
        {
            this.videoFilePath = videoPath;
            this.outputFilePath = outputPath;
            this.selectedStartTime = _selectedStartTime;
            this.audioHeight = _audioHeight;
            this.axisHeight = _axisHeight;
            this.timeFromMode = _timeFromMode;
            this.threshold = _threshold;
            this.GunTimeColor = _gunTimeColor;
        }


        public System.Drawing.Bitmap GetNthFrame(int frameIndex)
        {
            using var capture = new VideoCapture(videoFilePath);

            // Set the frame position
            capture.Set(VideoCaptureProperties.PosFrames, frameIndex);
            // Retrieve the frame
            Mat frame = new Mat();
            capture.Read(frame);
            // Only in Windows so OK.
#pragma warning disable CA1416 // Validate platform compatibility
            System.Drawing.Bitmap bitmap = BitmapConverter.ToBitmap(frame);
#pragma warning restore CA1416 // Validate platform compatibility
            return bitmap;
        }
        
        public double GetGunTimenFrameIndex(string _guninfoPath, VideoDetectMode vm = VideoDetectMode.FromFlash)
        {
            string guninfoFilePath = _guninfoPath;
            int index = 0;
            GunTime = 0.0;
            int Fps = 30; // Default FPS, can be adjusted based on the video file
            
            if (!File.Exists(videoFilePath))
            {
                Console.WriteLine("Gun info file not found. Please ensure 'guninfo.txt' exists in 'C:\\temp\\vid\\'.");
                return 0;
            }
            // Open the video file
            using (var capture = new VideoCapture(videoFilePath))
            {
                if (!capture.IsOpened())
                {
                    Console.WriteLine("Failed to open the video file.");
                    return 0;
                }
                Fps = (int)Math.Round(capture.Fps);
                //Fps = 30;
                // Check if the start time exceeds the video duration
                videoFrameCount = capture.FrameCount;
                videoDuration = videoFrameCount / capture.Fps; // Calculate total video duration in seconds
            }

            if (timeFromMode == TimeFromMode.FromGunviaAudio)
            {
                DetectAudioFlash.FFMpegActions.Filterdata(videoFilePath, guninfoFilePath);
                if (!File.Exists(guninfoFilePath))
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
                var normalizedData = data.Select(x => (double)(Amp * (x - min) / range)).ToArray();
                var expData1 = normalizedData.Select(x => Math.Round(Math.Pow(x, 10), 0)).ToArray(); // Exponential scaling for better visibility
                var maxx = expData1.Max();
                audioData = expData1.Select(x => audioHeight * x / maxx).ToArray(); // Ensure no negative values

                var average = Math.Round(audioData
                .OrderByDescending(n => n) // Sort in descending order
                .Skip(20)                 // Skip the top 20 values
                .Average(), 0);
                double thresholdLevel = audioData.Max() / (threshold);

                // Find the index of the first value greater than or equal to the threshold
                index = Array.FindIndex(audioData, x => x >= thresholdLevel);

                int count = audioData.Take(index + 1).Count(x => x >= audioData[index]);
                double r = audioData[index - 1] / (audioData.Max());
                System.Diagnostics.Debug.WriteLine($"Count: {count} Max {audioData.Max()}  ThresholdLevel {thresholdLevel} Indxex {index} Value {audioData[index]} Average{average}");
                                                                               //Audio frames are different to video frames
                ratio = (double)FFMpegActions.numAudioFrames / videoFrameCount;
                GunTimeIndex = (int)Math.Round(index / ratio);
                GunTime = (double)GunTimeIndex / Fps; // Convert frame index to time in seconds
                if (GunTime > videoDuration)
                {
                    Console.WriteLine($"Error: Start time ({GunTime:F2} seconds) exceeds video duration ({videoDuration:F2} seconds).");
                    return 0;
                }
                return GunTime; // Return the start frame index and time in seconds
            }
            else if (timeFromMode == TimeFromMode.FromGunViaVideo)
            {
                DetectVideoFlash.ActionVideoAnalysis actionVideoAnalysis
                    = new DetectVideoFlash.ActionVideoAnalysis(videoFilePath, (VideoDetectMode)vm, threshold);
                actionVideoAnalysis.ProcessVideo();
                GunTime = actionVideoAnalysis.GunTime;
                GunTimeIndex = actionVideoAnalysis.GunTimeIndex;

                var data = actionVideoAnalysis.VideoBrightnessData.ToArray(); // Get the video brightness data
 

                var min = data.Min();

                var max = data.Max();

                var range = max - min;

                int Amp = 10;

                // Normalize the data to a range of 0-Amp
                var normalizedData = data.Select(x => (double)(Amp * (x - min) / range)).ToArray();
                var expData1 = normalizedData.Select(x => Math.Round(Math.Pow(x, 10), 0)).ToArray(); // Exponential scaling for better visibility
                var maxx = expData1.Max();
                audioData = expData1.Select(x => audioHeight * x / maxx).ToArray(); // Ensure no negative values

                var average = Math.Round(audioData
                .OrderByDescending(n => n) // Sort in descending order
                .Skip(20)                 // Skip the top 20 values
                .Average(), 0);

                return GunTime;
            }
            else if (timeFromMode == TimeFromMode.FromButtonPress)
            {
                //Use video start
                GunTime = 0;
                GunTimeIndex = 0;
                return GunTime;
            }
            else if (timeFromMode == TimeFromMode.ManuallySelect)
            {
                // Use the selected start time
                GunTime = selectedStartTime;
                GunTimeIndex = (int)Math.Round(selectedStartTime * Fps);
                return GunTime;
            }
            return 0; // Return a tuple with zero values if no valid gun time is found
        }

        // Main method to start the stitching process
        public void Stitch()
        {
            PreviousStitchedImage = null;
            PreviousStitchedImageHeight = 0;
            if (!File.Exists(videoFilePath))
            {
                Console.WriteLine("Video file not found. Please ensure 'guninfo.txt' exists in 'C:\\temp\\vid\\'.");
                return;
            }
            if (timeFromMode == TimeFromMode.FromGunviaAudio)
            {
 
            }
            else if (timeFromMode== TimeFromMode.FromGunViaVideo)
            {

            }

            // Delete the file if it already exists
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
                System.Diagnostics.Debug.WriteLine($"Existing file at '{outputFilePath}' has been deleted.");
            }

            // Open the video file
            using var capture = new VideoCapture(videoFilePath);
            if (!capture.IsOpened())
            {
                Console.WriteLine("Failed to open the video file.");
                return;
            }

            Fps = (int)Math.Round(capture.Fps);
            //Fps = 30;

            // Check if the start time exceeds the video duration
            if (selectedStartTime > videoDuration)
            {
                Console.WriteLine($"Error: Start time ({selectedStartTime} seconds) exceeds video duration ({videoDuration:F2} seconds).");
                return;
            }

            // Move to the starting frame
            capture.Set(VideoCaptureProperties.PosFrames, 0);// GunTimeIndex);

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
            var stitchedImage = new Mat(stitchedHeight + 1 +  axisHeight, stitchedWidth, MatType.CV_8UC3, new Scalar(0, 0, 0)); // Extra 2 x 100 pixels for markers and audio graph

            switch (timeFromMode)
            {
                case TimeFromMode.FromButtonPress:
                case TimeFromMode.ManuallySelect:
                    stitchedImage = new Mat(stitchedHeight + 1 + axisHeight, stitchedWidth, MatType.CV_8UC3, new Scalar(0, 0, 0)); // Extra 2 x 100 pixels for markers and audio graph
                    break;
                case TimeFromMode.FromGunviaAudio:
                    stitchedImage = new Mat(stitchedHeight + 1 + audioHeight + axisHeight, stitchedWidth, MatType.CV_8UC3, new Scalar(0, 0, 0)); // Extra 2 x 100 pixels for markers and audio graph
                    break;
                case TimeFromMode.FromGunViaVideo:
                    stitchedImage = new Mat(stitchedHeight + 1 + audioHeight + axisHeight, stitchedWidth, MatType.CV_8UC3, new Scalar(0, 0, 0)); // Extra 2 x 100 pixels for markers and audio graph
                    break;
            }

            // Populate the stitched image with vertical lines
            for (int i = 0; i < verticalLines.Count; i++)
            {
                var column = verticalLines[i];
                column.CopyTo(stitchedImage.RowRange(0, stitchedHeight).Col(i)); // Copy column to the corresponding position

                int currentTimeSeconds = 0 /*selectedStartTime*/ + i / Fps; // Current time (relative to start)
                int currentMinute = currentTimeSeconds / OneMinute; // Current minute
                int currentSecond = currentTimeSeconds % OneMinute; // Seconds within the current minute

                // Add markers
                if (currentSecond == 0 && i % Fps == 0) // Red minute marker
                {
                    Cv2.Line(stitchedImage, new Point(i, stitchedHeight), new Point(i, stitchedHeight + axisHeight), new Scalar(0, 0, 255), 3); // Red line
                    if (i == 0) // First marker (special alignment)
                    {
                        int firstMinuteLabel = (selectedStartTime % OneMinute == 0) ? currentMinute : currentMinute - 1; // Adjust label only if start time is not an exact minute
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
                if (timeFromMode == TimeFromMode.FromGunviaAudio)
                {
                    int audioframe = (int)Math.Round(i * ratio);
                    int i2 = i;
                    if (i != 0)
                    {
                        i2 = i - 1;

                    }
                    int audioframe2 = (int)Math.Round(i2 * ratio);
                    Cv2.Line(stitchedImage, new Point(i2, stitchedHeight + axisHeight + audioHeight - audioData[audioframe2]), new Point(i, stitchedHeight + axisHeight + audioHeight - audioData[audioframe]), new Scalar(0, 255, 255), 1); // Read line
                }
                else if (timeFromMode == TimeFromMode.FromGunViaVideo)
                {
                    int i2 = i;
                    if (i != 0)
                    {
                        i2 = i - 1;

                    }                 
                    Cv2.Line(stitchedImage, new Point(i2, stitchedHeight + axisHeight + audioHeight - audioData[i2]), new Point(i, stitchedHeight + axisHeight + audioHeight - audioData[i]), new Scalar(0, 255, 255), 1); // Read line
                    
                }
            }

            if (GunTimeIndex != 0)
            {
                if (timeFromMode == TimeFromMode.FromGunviaAudio)
                {
                    Cv2.Line(stitchedImage, new Point(GunTimeIndex, 0), new Point(GunTimeIndex, stitchedHeight), GunTimeColor); // White line
                }
                else if (timeFromMode == TimeFromMode.FromGunViaVideo)
                {
                    Cv2.Line(stitchedImage, new Point(GunTimeIndex, 0), new Point(GunTimeIndex, stitchedHeight), GunTimeColor); // White line
                }
                else if (timeFromMode == TimeFromMode.ManuallySelect)
                {
                    Cv2.Line(stitchedImage, new Point(GunTimeIndex, 0), new Point(GunTimeIndex, stitchedHeight), GunTimeColor); // White line
                }
            }

            PreviousStitchedImage = stitchedImage;
            PreviousStitchedImageHeight = stitchedHeight;
            // Save the stitched image.
            Cv2.ImWrite(outputFilePath, stitchedImage);

            Console.WriteLine($"Stitched image with markers saved at '{outputFilePath}'.");
            return;
        }

        Mat? PreviousStitchedImage = null;
        int PreviousStitchedImageHeight = 0;
        public int AddGunLine(double _GunTimeDbl, Scalar _gunTimeColor)
        {
            this.GunTimeColor = _gunTimeColor;
            int _GunTimeIndex = (int)Math.Round(_GunTimeDbl * Fps);
            if (PreviousStitchedImage == null)
            {
                Console.WriteLine("No stitched image available to add gun line.");
                return 0;
            }
            if(PreviousStitchedImageHeight==0)
            {
                Console.WriteLine("No stitched image height available to add gun line.");
                return 0;
            }
            Cv2.Line(PreviousStitchedImage, new Point(_GunTimeIndex, 0), new Point(_GunTimeIndex, PreviousStitchedImageHeight), GunTimeColor); 

            // Save the stitched image.
            Cv2.ImWrite(outputFilePath, PreviousStitchedImage);

            Console.WriteLine($"Stitched image with markers saved at '{outputFilePath}'.");
            return _GunTimeIndex;
        }
   

        //private void StitchUpWorker(string videoPath, string outputPath, int startTimeSeconds, Action<IAsyncResult> callback)
        //{

        //    if (!File.Exists(videoPath))
        //    {
        //        File.Delete(outputPath);
        //    }


        //    // Show the busy indicator
        //    //BusyIndicator.Visibility = Visibility.Visible;
        //    //FinishTime.Visibility = Visibility.Hidden;
        //    //FinishTimeLabel.Visibility = FinishTime.Visibility;

        //    // Run the stitching process in a background thread
        //    BackgroundWorker worker = new BackgroundWorker();
        //    worker.DoWork += (s, args) =>
        //    {
        //        // Call the stitching process
        //        var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoPath,GunTimeIndex,GunTime,GunTimeColor, outputPath,"", startTimeSeconds);
        //        videoLength = videoStitcher.Stitch();
        //    };

        //    worker.RunWorkerCompleted += (s, args) =>
        //    {

        //        // Hide the busy indicator
        //        //BusyIndicator.Visibility = Visibility.Collapsed;

        //        // Display the stitched image
        //        if (File.Exists(outputPath))
        //        {
        //            //null, false, null)
        //            var argsx = new AsyncCompletedEventArgs(null, false, videoLength);
        //            callback.Invoke((IAsyncResult)argsx);
        //        }
        //        else
        //        {
                    
        //            //MessageBox.Show("Failed to create the stitched image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        //StitchButton.Visibility = Visibility.Visible; // Hide the button
        //        //StitchButton.Width = 200;
        //        //StitchButton.IsEnabled = true; // Re-enable the button
        //    };

        //    worker.RunWorkerAsync();
        //}

    }
}