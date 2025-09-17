using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhotoTimingDjaus.Enums;
using OpenCvSharp.Flann;
using System.Collections;
namespace DetectVideoFlash
{
    public class ActionVideoAnalysis
    {    // In your MainWindow.xaml.cs
        private FlashDetector flashDetector;
        private FrameChangeDetector frameChangeDetector;
        private MotionDetector motionDetector;
        private VideoCapture videoCapture;
        private double thresholdLevel;
        private VideoDetectMode videoDetectMode;
        public List<int> VideoBrightnessData = new List<int>();
        List<double> doubles = new List<double>();

        public double GunTime { get; set; } = 0;
        public int GunTimeIndex { get; set; }= 0;

        public  ActionVideoAnalysis(string VideoPath, VideoDetectMode _videoDetectMode,  double _thresholdLevel)
        {
            thresholdLevel = _thresholdLevel; // Use your threshold textbox value
            videoDetectMode = _videoDetectMode; // Use your VideoDetectMode enum value
            videoCapture = new VideoCapture(VideoPath);
            switch (videoDetectMode)
            {
                case VideoDetectMode.FromFlash:
                    // Initialize flash detector
                    flashDetector = new FlashDetector(thresholdLevel); // Use your threshold textbox
                    break;
                case VideoDetectMode.FromFrameChange:
                    // Initialize frame change detector
                    frameChangeDetector = new FrameChangeDetector(thresholdLevel);
                    break;
                case VideoDetectMode.FromMotionDetector:
                    // Initialize motion detector
                    motionDetector = new MotionDetector(thresholdLevel);
                    break;
                default:
                    throw new ArgumentException("Invalid video detect mode");
            }
        }

        public int GetFrameBrightnessEdgeChangeIndex(List<int> videoBrightnessData)
        {
            int min = videoBrightnessData.Min();
            int max = videoBrightnessData.Max();
            double threshold = max / 3.0;

            // Find the index of the first occurrence of the minimum value
            int minIndex = videoBrightnessData.IndexOf(min);

            // Search for the next value > threshold after minIndex
            int resultIndex = -1;
            for (int i = minIndex + 1; i < videoBrightnessData.Count; i++)
            {
                if (videoBrightnessData[i] > threshold)
                {
                    resultIndex = i;
                    break;
                }
            }
            return resultIndex;
        }

        public void ProcessVideo()
        {
            ScanVideo();
            int average;
            int max;
            int min;
            int median;
            average = (int)VideoBrightnessData.Average();
            var amax = (int)VideoBrightnessData.Max();
            var amin = (int)VideoBrightnessData.Min();
            var sorted = VideoBrightnessData.OrderBy(x => x).ToList();
            int amedian = sorted[sorted.Count / 2];

            int indx = GetFrameBrightnessEdgeChangeIndex(VideoBrightnessData);

            System.Diagnostics.Debug.WriteLine($"Edge: {indx}. {VideoBrightnessData[indx]}");
            for (int i=0; i<10;i++)
            {
                int id = indx + i - 4;
                System.Diagnostics.Debug.WriteLine($"About: {id}. {VideoBrightnessData[id]}");
            }

            var ints2 = VideoBrightnessData.Select(x => x >= average? x-average:0).ToList();
            max = ints2.Max();
            min = ints2.Min();
            GunTimeIndex = ints2.FindIndex(x => x >= ints2.Max() / 100);
            GunTimeIndex = indx;
            double fps = videoCapture.Get(VideoCaptureProperties.Fps);
            GunTime = GunTimeIndex / fps;

            median = VideoBrightnessData.OrderBy(x => x).ElementAt(doubles.Count / 2);
            System.Diagnostics.Debug.WriteLine($"GunTime: {GunTime} Average: {average} Min: {min} Max: {max} Median: {median}");
            foreach (int x in VideoBrightnessData)
            {
                System.Diagnostics.Debug.WriteLine(x);
            }
        }

        public void ScanVideo()
        {
            if (!videoCapture.IsOpened())
            {
                System.Diagnostics.Debug.WriteLine("Video capture not opened.");
                return;
            }

            using (Mat frame = new Mat())
            {
                while (videoCapture.Read(frame))
                {
                    // Process the frame as needed
                    // For example, you can display it or analyze it
                    // Cv2.ImShow("Frame", frame);
                    // Cv2.WaitKey(1); // Wait for a short time to display the frame
                    switch (videoDetectMode)
                    {
                        case VideoDetectMode.FromFlash:
                            // Initialize flash detector
                            int brightness = (int)Math.Round(flashDetector.DetectBrightness(frame),0);
                            VideoBrightnessData.Add(brightness);
                            break;
                        case VideoDetectMode.FromFrameChange:
                            // Initialize frame change detector
                            int diffs = frameChangeDetector.GetDiffs(frame);
                            VideoBrightnessData.Add(diffs);
                            break;
                        case VideoDetectMode.FromMotionDetector:
                            // Initialize motion detector
                            int motion = motionDetector.GetMotion(frame); 
                            VideoBrightnessData.Add(motion);
                            break;
                        default:
                            throw new ArgumentException("Invalid video detect mode");
                    }
                }
            }
        }



        public double ProcessVideox()
        {
            double frameTime = 0.0;
            if (!videoCapture.IsOpened())
                return frameTime;

            using (Mat frame = new Mat())
            {
                while (videoCapture.Read(frame))
                {
                    // Check for flash
                    if (flashDetector.DetectFlash(frame))
                    {
                        // Flash detected! Get current frame time
                        frameTime = videoCapture.Get(VideoCaptureProperties.PosMsec) / 1000.0;

                        // You might want to break here if you only need the first flash
                        break;
                    }

                    // Optional: Display the current frame for debugging
                    // You could convert the Mat to a BitmapSource and display it
                }
            }
            return frameTime;
        }


        public double ProcessVideoZZ()
        {
            double frameTime = 0.0;
            if (!videoCapture.IsOpened())
                return frameTime;

            using (Mat frame = new Mat())
            {
                while (videoCapture.Read(frame))
                {
                    var xx = motionDetector.GetMotion(frame);
                    System.Diagnostics.Debug.WriteLine(xx);
                    //// Check for flash
                    //if (frameChangeDetector.DetectSignificantChange(frame))
                    //{
                    //    // Flash detected! Get current frame time
                    //    var xx = VideoCaptureProperties.PosMsec;
                    //    frameTime = videoCapture.Get(VideoCaptureProperties.PosMsec) / 1000.0;

                    //    // You might want to break here if you only need the first flash
                    //    break;
                    //}

                    // Optional: Display the current frame for debugging
                    // You could convert the Mat to a BitmapSource and display it
                }
            }
            return frameTime;
        }

        public double ProcessVideov()
        {
            double frameTime = 0.0;
            if (!videoCapture.IsOpened())
                return frameTime;

            using (Mat frame = new Mat())
            {
                while (videoCapture.Read(frame))
                {
                    var xx = frameChangeDetector.GetDiffs(frame);
                    System.Diagnostics.Debug.WriteLine(xx);
                    //// Check for flash
                    //if (frameChangeDetector.DetectSignificantChange(frame))
                    //{
                    //    // Flash detected! Get current frame time
                    //    var xx = VideoCaptureProperties.PosMsec;
                    //    frameTime = videoCapture.Get(VideoCaptureProperties.PosMsec) / 1000.0;

                    //    // You might want to break here if you only need the first flash
                    //    break;
                    //}

                    // Optional: Display the current frame for debugging
                    // You could convert the Mat to a BitmapSource and display it
                }
            }
            return frameTime;
        }

        public double ProcessVideo3()
        {
            double frameTime = 0.0;
            if (!videoCapture.IsOpened())
                return frameTime;

            var info = new List<Tuple<double, double>>();

            using (Mat frame = new Mat())
            {
                while (videoCapture.Read(frame))
                {
                    
                    double brightness = flashDetector.DetectBrightness(frame);
                    frameTime = videoCapture.Get(VideoCaptureProperties.PosMsec) / 1000.0;
                    info.Add(new Tuple<double, double>(frameTime, brightness));
                }
            }


            double maxBrightness = info.Max(x => x.Item2);
            double minBrightness = info.Min(x => x.Item2);
            double averageBrightness = info.Average(x => x.Item2);

            var adj = info.Select(x => x.Item2 >= averageBrightness ? (x.Item2 - averageBrightness ): 0);
            var adjList = adj.ToList();
            foreach(double x in adjList)
            {
                System.Diagnostics.Debug.WriteLine(x);
            }

            System.Diagnostics.Debug.WriteLine($"Max Brightness: {maxBrightness} at {info[info.FindIndex(x => x.Item2 == maxBrightness)].Item1} seconds");
            System.Diagnostics.Debug.WriteLine($"Min Brightness: {minBrightness} at {info[info.FindIndex(x => x.Item2 == minBrightness)].Item1} seconds");
            System.Diagnostics.Debug.WriteLine($"Average Bightness {averageBrightness}");

            int indexMax = info.FindIndex(x => x.Item2 == info.Max(y => y.Item2));
            int indexMin = info.FindIndex(x => x.Item2 == info.Min(y => y.Item2));
            int indexAverage = info.FindIndex(x => x.Item2 == info.Average(y => y.Item2));
            System.Diagnostics.Debug.WriteLine($"Max Brightness: {info[indexMax].Item2} at {info[indexMax].Item1} seconds");
            System.Diagnostics.Debug.WriteLine($"Min Brightness: {info[indexMin].Item2} at {info[indexMin].Item1} seconds");
            return 0;
        }
    }
}
