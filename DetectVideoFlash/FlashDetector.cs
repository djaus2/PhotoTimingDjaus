using OpenCvSharp;

//namespace DetectVideoFlash
//{
public class FlashDetector
{
    private double brightnessThreshold;
    private Mat previousFrame;

    public FlashDetector(double brightnessThreshold = 50.0)
    {
        this.brightnessThreshold = brightnessThreshold;
    }

    public double DetectBrightness(Mat frame)
    {
        // Convert to grayscale
        Mat grayFrame = new Mat();
        Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

        // Calculate average brightness
        Scalar meanBrightness = Cv2.Mean(grayFrame);
        double currentBrightness = meanBrightness.Val0;
        return currentBrightness;
    }



    public bool DetectFlash(Mat frame)
    {
        // Convert to grayscale
        Mat grayFrame = new Mat();
        Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY);

        // Calculate average brightness
        double currentBrightness = DetectBrightness(grayFrame);
        System.Diagnostics.Debug.WriteLine(currentBrightness);

        if (previousFrame == null)
        {
            previousFrame = grayFrame.Clone();
            return false;
        }

        // Calculate previous frame brightness
        Scalar prevMeanBrightness = Cv2.Mean(previousFrame);
        double prevBrightness = prevMeanBrightness.Val0;

        // Update previous frame
        previousFrame = grayFrame.Clone();

        if((currentBrightness - prevBrightness) > brightnessThreshold)
        {

        }

        // Check if brightness increase exceeds threshold
        return (currentBrightness - prevBrightness) > brightnessThreshold;
    }

}