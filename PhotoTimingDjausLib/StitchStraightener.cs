using OpenCvSharp;
using System;
using System.Linq;

namespace PhotoTimingDjausLib
{ }


public static class StitchStraightener
{
    /// <summary>
    /// Iteratively straightens the bottom white line by vertical remaps only.
    /// </summary>
    /// <param name="inPath">input image path</param>
    /// <param name="outPath">output image path</param>
    /// <param name="maxIters">max number of refinement passes</param>
    /// <param name="tolerancePx">
    /// stop when the largest deviation (in pixels) along the bottom line is ≤ this
    /// </param>
    public static void StraightenBottomIterative(
        string inPath,
        string outPath,
        int maxIters = 5,
        double tolerancePx = 1.0)
    {
        // Load once
        Mat current = Cv2.ImRead(inPath, ImreadModes.Color);
        int w = current.Width, h = current.Height;

        for (int iter = 0; iter < maxIters; iter++)
        {
            // 1) Detect white-line Y per column
            var lineY = DetectBottomLineY(current);

            // 2) Compute baseline & residuals
            int median = Median(lineY);
            var deviations = lineY.Select(y => Math.Abs(y - median)).ToArray();
            double maxDev = deviations.Max();

            Console.WriteLine($"Pass #{iter + 1}: max deviation = {maxDev:F2}px");
            if (maxDev <= tolerancePx)
            {
                Console.WriteLine("Tolerance met — stopping iterations.");
                break;
            }

            // 3) Build remap (vertical shifts only)
            var mapX = new Mat(h, w, MatType.CV_32FC1);
            var mapY = new Mat(h, w, MatType.CV_32FC1);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float offset = lineY[x] - median;
                    mapX.Set(y, x, x);
                    mapY.Set(y, x, y - offset);
                }
            }

            // 4) Apply remap to generate a new “current” for the next pass
            var next = new Mat();
            Cv2.Remap(current, next, mapX, mapY, InterpolationFlags.Linear, BorderTypes.Constant);
            current = next;
        }

        // Save final result
        Cv2.ImWrite(outPath, current);
    }

    /// <summary>
    /// Scans each column bottom-up until a near-white pixel is found.
    /// (Customize your threshold or even sub-pixel fitting here.)
    /// </summary>
    private static int[] DetectBottomLineY(Mat src, byte threshold = 240)
    {
        int w = src.Width, h = src.Height;
        var lineY = new int[w];

        for (int x = 0; x < w; x++)
        {
            lineY[x] = h - 1;
            for (int y = h - 1; y >= 0; y--)
            {
                Vec3b p = src.At<Vec3b>(y, x);
                if (p.Item0 >= threshold && p.Item1 >= threshold && p.Item2 >= threshold)
                {
                    lineY[x] = y;
                    break;
                }
            }
        }

        return lineY;
    }

    private static int Median(int[] data)
    {
        var sorted = (int[])data.Clone();
        Array.Sort(sorted);
        return sorted[sorted.Length / 2];
    }
}


