using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhotoTimingDjausLib
{


    public static class StitchStraightenerofMat
    {
        /// <summary>
        /// Straightens the bottom white line in each Mat of the input list,
        /// by repeatedly applying a vertical-only remap until flat enough.
        /// </summary>
        /// <param name="srcList">List of stitched-frame Mats to correct</param>
        /// <param name="maxIters">Maximum passes per frame</param>
        /// <param name="tolerancePx">
        /// Stop when the largest deviation along the bottom line ≤ this (in pixels)
        /// </param>
        /// <returns>New List of straightened Mats (same order as srcList)</returns>
        public static List<Mat> LevelBottomIterative(
            List<Mat> srcList,
            int maxIters = 5,
            double tolerancePx = 1.0)
        {
            var result = new List<Mat>(srcList.Count);

            foreach (var src in srcList)
            {
                // Work on a clone so we don't mutate the caller's Mat
                var current = src.Clone();
                int w = current.Width, h = current.Height;

                for (int iter = 0; iter < maxIters; iter++)
                {
                    // 1) detect per-column bottom-white Y
                    var lineY = DetectBottomLineY(current);

                    // 2) compute median baseline and max deviation
                    int median = Median(lineY);
                    double maxDev = lineY.Select(y => Math.Abs(y - median)).Max();

                    if (maxDev <= tolerancePx)
                        break;

                    // 3) build vertical-only remap
                    var mapX = new Mat(h, w, MatType.CV_32FC1);
                    var mapY = new Mat(h, w, MatType.CV_32FC1);

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            mapX.Set(y, x, x);
                            float offset = lineY[x] - median;
                            mapY.Set(y, x, y - offset);
                        }
                    }

                    // 4) apply remap for next iteration
                    var next = new Mat();
                    Cv2.Remap(current, next, mapX, mapY,
                              InterpolationFlags.Linear,
                              BorderTypes.Constant);
                    current.Dispose();
                    current = next;
                }

                result.Add(current);
            }

            return result;
        }

        /// <summary>
        /// Scans each column bottom-up until a near-white pixel is found.
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
                    if (p.Item0 >= threshold &&
                        p.Item1 >= threshold &&
                        p.Item2 >= threshold)
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

}
