using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace INFOIBV
{
    /// <summary>
    /// Represents a connected region in a binary image, storing its properties and shape descriptors
    /// </summary>
    public struct Region
    {
        /// <summary>
        /// The unique label identifying this region
        /// </summary>
        public int Label { get; set; }

        /// <summary>
        /// List of pixel coordinates (Y,X) that make up this region
        /// </summary>
        public List<(int Y, int X)> Pixels { get; set; }

        /// <summary>
        /// List of pixel coordinates forming the outer boundary of the region
        /// </summary>
        public List<(int Y, int X)> OuterContour { get; set; }

        /// <summary>
        /// The number of pixels in the region
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// The length of the outer contour
        /// </summary>
        public double Perimeter { get; set; }

        /// <summary>
        /// Measure of how circular the region is (4πA/P²)
        /// </summary>
        public double Circularity { get; set; }

        /// <summary>
        /// Central moments of the region (M20, M02, M11)
        /// </summary>
        public (double M20, double M02, double M11) CentralMoments { get; set; }

        /// <summary>
        /// Center of mass of the region (Y,X)
        /// </summary>
        public (double Y, double X) Centroid { get; set; }

        /// <summary>
        /// Ratio of major axis length to minor axis length
        /// </summary>
        public double Elongation { get; set; }

        /// <summary>
        /// Creates a new region with the given label
        /// </summary>
        /// <param name="label">The label to assign to this region</param>
        public Region(int label)
        {
            Label = label;
            Pixels = new List<(int Y, int X)>();
            OuterContour = new List<(int Y, int X)>();
            Area = 0;
            Perimeter = 0;
            Circularity = 0;
            CentralMoments = (0, 0, 0);
            Centroid = (0, 0);
            Elongation = 0;
        }
    }

    /// <summary>
    /// Provides methods for region detection and analysis in binary images
    /// </summary>
    public class Regions
    {
        // Directions for 8-connectivity
        private static readonly (int Y, int X)[] Directions = {
            (0, 1), (1, 1), (1, 0), (1, -1),
            (0, -1), (-1, -1), (-1, 0), (-1, 1)
        };

        /// <summary>
        /// Finds and analyzes all connected regions in a binary image
        /// </summary>
        /// <param name="binaryImage">The input binary image (255 for foreground, 0 for background)</param>
        /// <returns>A list of detected and analyzed regions</returns>
        public static List<Region> FindRegions(byte[,] binaryImage)
        {
            int height = binaryImage.GetLength(0);
            int width = binaryImage.GetLength(1);
            if (!BaseFunctions.isBinaryImage(binaryImage))
            {
                throw new ArgumentException("Input Image isnt binary");
            }
            int[,] labels = new int[height, width];
            int nextLabel = 1;
            Dictionary<int, int> equivalences = new Dictionary<int, int>();

            // First pass: assign initial labels
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (binaryImage[y, x] == 255)
                    {
                        var neighborLabels = GetNeighborLabels(labels, y, x);

                        if (neighborLabels.Count == 0)
                        {
                            labels[y, x] = nextLabel++;
                        }
                        else
                        {
                            int minLabel = neighborLabels.Min();
                            labels[y, x] = minLabel;

                            foreach (int label in neighborLabels)
                            {
                                if (label != minLabel)
                                {
                                    UpdateEquivalences(equivalences, label, minLabel);
                                }
                            }
                        }
                    }
                }
            }

            // second pass: collect pixels for each region
            Dictionary<int, List<(int Y, int X)>> regionPixels = new Dictionary<int, List<(int Y, int X)>>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (labels[y, x] > 0)
                    {
                        int finalLabel = GetFinalLabel(equivalences, labels[y, x]);
                        if (!regionPixels.ContainsKey(finalLabel))
                        {
                            regionPixels[finalLabel] = new List<(int Y, int X)>();
                        }
                        regionPixels[finalLabel].Add((y, x));
                    }
                }
            }

            // Create and analyze regions
            List<Region> regions = new List<Region>();
            foreach (var kvp in regionPixels)
            {
                Region region = new Region(kvp.Key);
                region.Pixels = kvp.Value;
                region = AnalyzeRegion(region, binaryImage);
                regions.Add(region);
            }

            return regions;
        }

     
        /// <summary>
        /// Gets the labels of neighboring pixels in an 8-connected neighborhood
        /// </summary>
        private static List<int> GetNeighborLabels(int[,] labels, int y, int x)
        {
            List<int> neighbors = new List<int>();
            // Check only previous neighbors (top-left, top, top-right, left)
            for (int dy = -1; dy <= 0; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dy == 0 && dx >= 0) break;

                    int ny = y + dy;
                    int nx = x + dx;

                    if (ny >= 0 && nx >= 0 && ny < labels.GetLength(0) && nx < labels.GetLength(1))
                    {
                        int label = labels[ny, nx];
                        if (label > 0) neighbors.Add(label);
                    }
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Updates the equivalence classes between labels
        /// </summary>
        private static void UpdateEquivalences(Dictionary<int, int> equivalences, int label1, int label2)
        {
            if (!equivalences.ContainsKey(label1)) equivalences[label1] = label1;
            if (!equivalences.ContainsKey(label2)) equivalences[label2] = label2;

            int root1 = GetFinalLabel(equivalences, label1);
            int root2 = GetFinalLabel(equivalences, label2);

            if (root1 != root2)
            {
                equivalences[root1] = root2;
            }
        }

        /// <summary>
        /// Gets the final label for a given label after resolving all equivalences
        /// </summary>
        private static int GetFinalLabel(Dictionary<int, int> equivalences, int label)
        {
            int root = label;
            while (equivalences.ContainsKey(root) && equivalences[root] != root)
            {
                root = equivalences[root];
            }
            return root;
        }

        /// <summary>
        /// Traces the boundary of a region starting from a given point
        /// </summary>
        /// <param name="binaryImage">The binary image containing the region</param>
        /// <param name="startPoint">The starting point for tracing</param>
        /// <returns>List of boundary pixel coordinates</returns>
        private static List<(int Y, int X)> TraceBoundary(byte[,] binaryImage, (int Y, int X) startPoint)
        {
            List<(int Y, int X)> boundary = new List<(int Y, int X)>();
            (int Y, int X) current = startPoint;
            int direction = 7; // Start searching at 7 (top-right) for outer boundary

            do
            {
                boundary.Add(current);

                // Find next boundary pixel
                bool found = false;
                int newDirection = direction;

                for (int i = 0; i < 8; i++)
                {
                    newDirection = (direction + 6 + i) % 8; // Search counter-clockwise
                    int newY = current.Y + Directions[newDirection].Y;
                    int newX = current.X + Directions[newDirection].X;

                    if (newY >= 0 && newY < binaryImage.GetLength(0) &&
                        newX >= 0 && newX < binaryImage.GetLength(1) &&
                        binaryImage[newY, newX] == 255)
                    {
                        current = (newY, newX);
                        direction = newDirection;
                        found = true;
                        break;
                    }
                }

                if (!found) break;

            } while (!current.Equals(startPoint));

            return boundary;
        }

        /// <summary>
        /// Draws regions with different colors in a color image
        /// </summary>
        /// <param name="regions">List of regions to draw</param>
        /// <param name="height">Height of the output image</param>
        /// <param name="width">Width of the output image</param>
        /// <returns>Color image with colored regions</returns>
        public static Color[,] DrawRegions(List<Region> regions, int height, int width)
        {
            Color[,] output = new Color[height, width];
            Random rand = new Random(12); 

            foreach (var region in regions)
            {
                Color regionColor = Color.FromArgb(
                    rand.Next(0, 200),
                    rand.Next(0, 200),
                    rand.Next(0, 200)
                );

                foreach (var pixel in region.Pixels)
                {
                    output[pixel.Y, pixel.X] = regionColor;
                }
            }

            return output;
        }



        #region region analysis functions
        /// <summary>
        /// Analyzes a region to calculate its properties and shape descriptors
        /// </summary>
        /// <param name="region">The region to analyze</param>
        /// <param name="binaryImage">The original binary image</param>
        /// <returns>The analyzed region with all properties calculated</returns>
        private static Region AnalyzeRegion(Region region, byte[,] binaryImage)
        {
            // Basic properties
            region.Area = region.Pixels.Count;
            region.OuterContour = TraceBoundary(binaryImage, region.Pixels[0]);
            region.Perimeter = region.OuterContour.Count;
            region.Centroid = CalculateCentroid(region);
            region.CentralMoments = CalculateCentralMoments(region);
            region.Elongation = CalculateElongation(region.CentralMoments);
            region.Circularity = CalculateCircularity(region);

            return region;
        }

        /// <summary>
        /// Calculates the centroid of the region
        /// </summary>
        private static (double Y, double X) CalculateCentroid(Region region)
        {
            double sumX = 0, sumY = 0;
            foreach (var pixel in region.Pixels)
            {
                sumY += pixel.Y;
                sumX += pixel.X;
            }
            return (sumY / region.Area, sumX / region.Area);
        }

        /// <summary>
        /// Calculates the central moments of the region
        /// </summary>
        private static (double m20, double m02, double m11) CalculateCentralMoments(Region region)
        {
            double m20 = 0, m02 = 0, m11 = 0;
            foreach (var pixel in region.Pixels)
            {
                double dy = pixel.Y - region.Centroid.Y;
                double dx = pixel.X - region.Centroid.X;
                m20 += dy * dy;
                m02 += dx * dx;
                m11 += dx * dy;
            }

            // Normalize moments by area
            return (m20 / region.Area, m02 / region.Area, m11 / region.Area);
        }

      

        /// <summary>
        /// Calculates the elongation using eigenvalues
        /// </summary>
        private static double CalculateElongation((double m20, double m02, double m11) moments)
        {
            double delta = Math.Sqrt(4 * moments.m11 * moments.m11 +
                                   (moments.m20 - moments.m02) * (moments.m20 - moments.m02));
            double lambda1 = (moments.m20 + moments.m02 + delta) / 2;
            double lambda2 = (moments.m20 + moments.m02 - delta) / 2;
            return Math.Sqrt(lambda1 / lambda2);
        }

      

        /// <summary>
        /// Calculates the rectangularity of the region
        /// </summary>
        private static double CalculateRectangularity(
            Region region,
            (double Length, double Width) boundingRect)
        {
            return region.Area / (boundingRect.Length * boundingRect.Width);
        }

        /// <summary>
        /// Calculates the circularity of the region
        /// </summary>
        private static double CalculateCircularity(Region region)
        {
            return (4 * Math.PI * region.Area) / (region.Perimeter * region.Perimeter);
        }
        #endregion

       
    }
}