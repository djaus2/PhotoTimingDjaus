using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace DetectVideoFlash
{
    public class MotionDetector
    {
        private BackgroundSubtractorMOG2 backgroundSubtractor;
        private double threshold;
        private Mat previousFrame = null;
        private int frameCount = 0;

        public MotionDetector(double threshold = 500.0)
        {
            // Reduce history for faster adaptation, lower varThreshold for more sensitivity
            backgroundSubtractor = BackgroundSubtractorMOG2.Create(history: 200, varThreshold: 8, detectShadows: false);
            this.threshold = threshold;
        }

        public bool DetectMotion(Mat frame)
        {
            int motionPixels = GetMotion(frame);
            // Return true if motion exceeds threshold
            return motionPixels > threshold;
        }

        public int GetMotion(Mat frame)
        {
            frameCount++;
            
            // Convert frame to grayscale for better motion detection
            Mat grayFrame = new Mat();
            Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);
            
            // Apply Gaussian blur to reduce noise
            Cv2.GaussianBlur(grayFrame, grayFrame, new Size(21, 21), 0);
            
            // Initialize previousFrame if it's the first frame
            if (previousFrame == null)
            {
                previousFrame = grayFrame.Clone();
                return 0; // No motion on first frame
            }
            
            // Calculate absolute difference between current and previous frame
            Mat frameDelta = new Mat();
            Cv2.Absdiff(previousFrame, grayFrame, frameDelta);
            
            // Apply background subtraction as well
            Mat foregroundMask = new Mat();
            backgroundSubtractor.Apply(frame, foregroundMask);
            
            // Combine both methods for better detection
            Mat combinedMask = new Mat();
            Cv2.BitwiseOr(frameDelta, foregroundMask, combinedMask);

            // Apply threshold to highlight changes
            Cv2.Threshold(combinedMask, combinedMask, 25, 255, ThresholdTypes.Binary);

            // Apply morphological operations to remove noise
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Cv2.MorphologyEx(combinedMask, combinedMask, MorphTypes.Dilate, kernel, iterations: 2);

            // Count non-zero pixels (motion pixels)
            int motionPixels = Cv2.CountNonZero(combinedMask);
            
            // Update previous frame (every 3rd frame to allow for more change detection)
            if (frameCount % 3 == 0)
            {
                previousFrame = grayFrame.Clone();
            }
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Motion pixels: {motionPixels}");
            
            return motionPixels;
        }
    }
}
