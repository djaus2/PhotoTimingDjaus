using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace DetectVideoFlash
{
    public class FrameChangeDetector
    {
        private Mat previousFrame;
        private double threshold;

        public FrameChangeDetector(double threshold = 30.0)
        {
            this.threshold = threshold;
        }

        public int GetDiffs(Mat currentFrame)
        {
            if (previousFrame == null)
            {
                previousFrame = currentFrame.Clone();
                return 0;
            }

            // Convert frames to grayscale for simpler comparison
            Mat grayPrevious = new Mat();
            Mat grayCurrent = new Mat();
            Cv2.CvtColor(previousFrame, grayPrevious, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(currentFrame, grayCurrent, ColorConversionCodes.BGR2GRAY);

            // Calculate absolute difference between frames
            Mat frameDiff = new Mat();
            Cv2.Absdiff(grayPrevious, grayCurrent, frameDiff);

            // Apply threshold to identify significant changes
            Mat thresholdedDiff = new Mat();
            Cv2.Threshold(frameDiff, thresholdedDiff, threshold, 255, ThresholdTypes.Binary);

            // Count non-zero pixels (changed pixels)
            int changedPixels = Cv2.CountNonZero(thresholdedDiff);
            previousFrame = currentFrame;
            return changedPixels;
        }

        public bool DetectSignificantChange(Mat currentFrame)
        {
            if (previousFrame == null)
            {
                previousFrame = currentFrame.Clone();
                return false;
            }

            // Convert frames to grayscale for simpler comparison
            Mat grayPrevious = new Mat();
            Mat grayCurrent = new Mat();
            Cv2.CvtColor(previousFrame, grayPrevious, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(currentFrame, grayCurrent, ColorConversionCodes.BGR2GRAY);

            // Calculate absolute difference between frames
            Mat frameDiff = new Mat();
            Cv2.Absdiff(grayPrevious, grayCurrent, frameDiff);

            // Apply threshold to identify significant changes
            Mat thresholdedDiff = new Mat();
            Cv2.Threshold(frameDiff, thresholdedDiff, threshold, 255, ThresholdTypes.Binary);

            // Count non-zero pixels (changed pixels)
            int changedPixels = Cv2.CountNonZero(thresholdedDiff);

            // Calculate percentage of changed pixels
            double changePercentage = (double)changedPixels / (thresholdedDiff.Rows * thresholdedDiff.Cols) * 100;

            // Update previous frame
            previousFrame = currentFrame.Clone();

            // Return true if change percentage exceeds threshold
            return changePercentage > 5.0; // Adjust this percentage as needed
        }
    }
}
