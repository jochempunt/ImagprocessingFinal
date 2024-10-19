using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    /// <summary>
    /// Provides comprehensive histogram analysis functionality for image processing.
    /// Supports generation and analysis of image histograms, including statistics and manipulations.
    /// </summary>
    public class Histogramms
    {
        /// <summary>
        /// Stores histogram data and analysis results for an image.
        /// </summary>
        public struct HistogramData
        {
            /// <summary>
            /// Number of distinct intensity values present in the image
            /// </summary>
            public int DistinctValues { get; set; }

            /// <summary>
            /// Mapping of intensity values (0-255) to their frequencies
            /// </summary>
            public Dictionary<byte, int> Distribution { get; set; }

            /// <summary>
            /// Total number of pixels in the image
            /// </summary>
            public int TotalPixels { get; set; }

            /// <summary>
            /// Mean intensity value
            /// </summary>
            public double Mean { get; set; }
        }

        /// <summary>
        /// Computes comprehensive histogram data for an input image.
        /// </summary>
        /// <param name="image">Input image as 2D byte array</param>
        /// <returns>HistogramData containing distribution and statistical measures</returns>
        /// <exception cref="ArgumentNullException">Thrown when image is null</exception>
        public static HistogramData ComputeHistogram(byte[,] image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var histogram = new Dictionary<byte, int>();
            long sum = 0;
            int maxFrequency = 0;

            // Calculate basic histogram and statistics
            foreach (byte value in image)
            {
                if (histogram.ContainsKey(value))
                {
                    histogram[value]++;
                }
                else
                {
                    histogram[value] = 1;
                }

                // Track mode
                if (histogram[value] > maxFrequency)
                {
                    maxFrequency = histogram[value];
                }

                sum += value;
            }

            int totalPixels = image.GetLength(0) * image.GetLength(1);

            return new HistogramData
            {
                Distribution = histogram,
                DistinctValues = histogram.Count,
                TotalPixels = totalPixels,
                Mean = (double)sum / totalPixels
            };
        }

        /// <summary>
        /// Calculates the cumulative histogram from a regular histogram.
        /// </summary>
        /// <param name="histogramData">Input histogram data</param>
        /// <returns>Dictionary mapping intensity values to cumulative frequencies</returns>
        public static Dictionary<byte, int> ComputeCumulativeHistogram(HistogramData histogramData)
        {
            var cumulative = new Dictionary<byte, int>();
            int runningSum = 0;

            for (byte i = 0; i <= 255; i++)
            {
                if (histogramData.Distribution.ContainsKey(i))
                {
                    runningSum += histogramData.Distribution[i];
                }
                cumulative[i] = runningSum;
            }

            return cumulative;
        }

        /// <summary>
        /// Normalizes histogram frequencies to the range [0.0, 1.0].
        /// </summary>
        /// <param name="histogramData">Input histogram data</param>
        /// <returns>Dictionary mapping intensity values to normalized frequencies</returns>
        public static Dictionary<byte, double> NormalizeHistogram(HistogramData histogramData)
        {
            var normalized = new Dictionary<byte, double>();
            double totalPixels = histogramData.TotalPixels;

            foreach (var kvp in histogramData.Distribution)
            {
                normalized[kvp.Key] = kvp.Value / totalPixels;
            }

            return normalized;
        }

        
    }
}
