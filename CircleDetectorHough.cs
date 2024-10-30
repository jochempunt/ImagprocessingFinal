using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    internal class CircleDetectorHough
    {
        public class Circle
        {
            public int CenterX { get; }
            public int CenterY { get; }
            public int Radius { get; }

            public Circle(int centerX, int centerY, int radius)
            {
                CenterX = centerX;
                CenterY = centerY;
                Radius = radius;
            }
        }


        private static List<Circle> DetectCircles(byte[,] inputImage, float minRadius, float maxRadius, float radiusStep, float thresholdPercentage)
        {
            List<Circle> foundCircles = new List<Circle>();
            int[,,] accumulator = houghTransformCircles(inputImage, minRadius, maxRadius, radiusStep);

            int[,,] supressedAccumulator = nonMaximumSuppression(accumulator);

            int maxAccumulatorValue = findMaxAccumulatorValue(accumulator);
            Console.WriteLine("max accum value: " + maxAccumulatorValue);
            if (maxAccumulatorValue == 0)
            {
                return foundCircles;
            }
            int thresholdValue = (int)Math.Round(thresholdPercentage * maxAccumulatorValue);
            List<Circle> initalCircles = findCircles(supressedAccumulator, minRadius, radiusStep, thresholdValue);
            Console.WriteLine("amount of circles detected: " + initalCircles.Count);
            foundCircles = MergeSimilarCircles(initalCircles,((inputImage.GetLength(0) + inputImage.GetLength(1))/2 )* 0.15f);
            Console.WriteLine("amount of distinct circles detected: " + foundCircles.Count);
            // now check if circles have very similar center position, then check if they are similar size

            return foundCircles;
        }


        public static List<Circle> findCircles(byte[,]greyScaleImage)
        {
            byte[,] SymbolEdges = EdgeDetector.detectEdgesCanny(greyScaleImage, 40, 130, 1f, 3);

            int width = greyScaleImage.GetLength(1);
            int height = greyScaleImage.GetLength(0);
            float arithmeticMean = (width + height) / 2f;
            // Arithmetic mean based
            float minRadiusArithmetic = (int)Math.Round(arithmeticMean * 0.16);
            float maxRadiusArithmetic = (int)Math.Round(arithmeticMean * 0.3);

            // Apply safety bounds to your actual used values
            minRadiusArithmetic = Math.Max(minRadiusArithmetic, 3);
            

            // Debug logging
            Console.WriteLine("=== Debug Information ===");
            Console.WriteLine($"Image dimensions: ({width}, {height})");
            Console.WriteLine($"min and max radi: ({minRadiusArithmetic}, {maxRadiusArithmetic})");


            return CircleDetectorHough.DetectCircles(SymbolEdges, minRadiusArithmetic, maxRadiusArithmetic, 0.5f, 0.75f);
        }


        private static List<Circle> MergeSimilarCircles(List<Circle> circles,double centerDistanceThreshold =5f)
        {
            List<Circle> mergedCircles = new List<Circle>();
            List<bool> processed = new List<bool>(new bool[circles.Count]);

            // Parameters for similarity
            double radiusRatioThreshold = 0.3;    // Maximum relative difference in radius (20%)

            for (int i = 0; i < circles.Count; i++)
            {
                if (processed[i]) continue;

                List<Circle> similarCircles = new List<Circle>();
                similarCircles.Add(circles[i]);
                processed[i] = true;

                // Find all circles similar to circles[i]
                for (int j = i + 1; j < circles.Count; j++)
                {
                    if (processed[j]) continue;

                    double centerDistance = CalculateCenterDistance(circles[i], circles[j]);
                    double radiusRatio = Math.Abs(circles[i].Radius - circles[j].Radius) / Math.Max(circles[i].Radius, circles[j].Radius);

                    if (centerDistance <= centerDistanceThreshold && radiusRatio <= radiusRatioThreshold)
                    {
                        similarCircles.Add(circles[j]);
                        processed[j] = true;
                    }
                }

                // Merge similar circles into one
                if (similarCircles.Count > 0)
                {
                    Circle mergedCircle = MergeCircleGroup(similarCircles);
                    mergedCircles.Add(mergedCircle);
                }
            }

            return mergedCircles;
        }

        private static double CalculateCenterDistance(Circle c1, Circle c2)
        {
            double dx = c1.CenterX - c2.CenterX;
            double dy = c1.CenterY - c2.CenterY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static Circle MergeCircleGroup(List<Circle> circles)
        {
            if (circles.Count == 1) return circles[0];

            // Average center coordinates and radius
            double sumX = 0, sumY = 0, sumR = 0;
            foreach (var circle in circles)
            {
                sumX += circle.CenterX;
                sumY += circle.CenterY;
                sumR += circle.Radius;
            }

            

            (int Y, int X) mergedCenter = ((int)sumY / circles.Count, (int)sumX / circles.Count);

            float mergedRadius = (float)(sumR / circles.Count);

            return new Circle(mergedCenter.X,mergedCenter.Y,(int)Math.Round(mergedRadius));
        }




        public static Color[,] DrawCircles(List<Circle> circles, Color[,] image)
        {
            Color[,] outputImage = (Color[,])image.Clone();// Clone the original image to draw on

            foreach (Circle circle in circles)
            {
                DrawCircle(circle, outputImage);
            }

            return outputImage;
        }

        private static void DrawCircle(Circle circle, Color[,] image)
        {
            int centerY = circle.CenterY;
            int centerX = circle.CenterX;
            int radius = circle.Radius;

            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius; // Starting decision parameter for Bresenham's algorithm

            while (y >= x)
            {
                DrawCirclePoints(centerX, centerY, x, y, image);
                x++;
                if (d > 0)
                {
                    y--;
                    d = d + 4 * (x - y) + 10;
                }
                else
                {
                    d = d + 4 * x + 6;
                }
            }
        }

        private static void DrawCirclePoints(int centerX, int centerY, int x, int y, Color[,] image)
        {
            MarkPixel(centerX + x, centerY + y, image);
            MarkPixel(centerX - x, centerY + y, image);
            MarkPixel(centerX + x, centerY - y, image);
            MarkPixel(centerX - x, centerY - y, image);
            MarkPixel(centerX + y, centerY + x, image);
            MarkPixel(centerX - y, centerY + x, image);
            MarkPixel(centerX + y, centerY - x, image);
            MarkPixel(centerX - y, centerY - x, image);
        }

        private static void MarkPixel(int x, int y, Color[,] image)
        {
            if (x >= 0 && x < image.GetLength(1) && y >= 0 && y < image.GetLength(0))
            {
                image[y, x] = Color.Blue; // Set pixel to white or your desired color for drawing
            }
        }


        private static int findMaxAccumulatorValue(int[,,] accumulator)
        {
            int maxAccumulatorValue = 0;
            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);
            int radiusCount = accumulator.GetLength(2);

            for (int r = 0; r < radiusCount; r++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (accumulator[y, x, r] > maxAccumulatorValue)
                        {
                            maxAccumulatorValue = accumulator[y, x, r];
                        }
                    }
                }
            }

            return maxAccumulatorValue;
        }


        private static int[,,] houghTransformCircles(byte[,] inputImage, float minRadius, float maxRadius, float radiusStep)
        {
            int imgHeight = inputImage.GetLength(0);
            int imgWidth = inputImage.GetLength(1);
            int radiusRange = (int)Math.Round((maxRadius - minRadius) / radiusStep + 1.0f);

            // accumulator to store votes: [centerY, centerX, radiusIndex]
            int[,,] accumulator = new int[imgHeight, imgWidth, radiusRange];


            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    // check if  pixel is an edge pixel
                    if (inputImage[y, x] > 0)
                    {

                        for (int ri = 0; ri < radiusRange; ri++)
                        {

                            int radius = (int)(minRadius + ri * radiusStep);

                            // use Bresenham's algorithm to accumulate along the circle perimeter
                            bresenhamCircleAccumulateVotes(x, y, radius, accumulator, ri);
                        }
                    }
                }
            }

            return accumulator;
        }

        private static List<Circle> findCircles(int[,,] accumulator, float minRadius, float radiusStep, int threshold)
        {
            List<Circle> detectedCircles = new List<Circle>();
            int width = accumulator.GetLength(0);
            int height = accumulator.GetLength(1);
            int radiusRange = accumulator.GetLength(2);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int r = 0; r < radiusRange; r++)
                    {
                        if (accumulator[x, y, r] >= threshold)
                        {
                            int radius = (int)(minRadius + r * radiusStep);
                            detectedCircles.Add(new Circle(y, x, radius));
                        }
                    }
                }
            }

            return detectedCircles;
        }

        private static void bresenhamCircleAccumulateVotes(int centerX, int centerY, int radius, int[,,] accumulator, int radiusIndex)
        {
            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius;  // starting decision parameter for Bresenhams algorithm


            bresenhamVoteForCircleCenter(centerX, centerY, x, y, accumulator, radiusIndex);

            // bresenham stopping condition
            while (y >= x)
            {
                x++;
                // update the decision parameter and adjust y
                if (d > 0)
                {
                    y--;
                    d = d + 4 * (x - y) + 10;
                }
                else
                {
                    d = d + 4 * x + 6;
                }
                bresenhamVoteForCircleCenter(centerX, centerY, x, y, accumulator, radiusIndex);
            }
        }

        // accumulate votes for potential circle centers using 8-way symmetry
        private static void bresenhamVoteForCircleCenter(int cx, int cy, int x, int y, int[,,] accumulator, int radiusIndex)
        {
            // Accumulate votes for all eight points of the circle
            incrementAccumulator(cx + x, cy + y, accumulator, radiusIndex);
            incrementAccumulator(cx - x, cy + y, accumulator, radiusIndex);
            incrementAccumulator(cx + x, cy - y, accumulator, radiusIndex);
            incrementAccumulator(cx - x, cy - y, accumulator, radiusIndex);
            incrementAccumulator(cx + y, cy + x, accumulator, radiusIndex);
            incrementAccumulator(cx - y, cy + x, accumulator, radiusIndex);
            incrementAccumulator(cx + y, cy - x, accumulator, radiusIndex);
            incrementAccumulator(cx - y, cy - x, accumulator, radiusIndex);
        }

        // Increment the vote in the accumulator for valid points
        private static void incrementAccumulator(int x, int y, int[,,] accumulator, int radiusIndex)
        {
            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);

            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                accumulator[y, x, radiusIndex]++;
            }
        }







        // Perform non-maximum suppression on the accumulator
        private static int[,,] nonMaximumSuppression(int[,,] accumulator3D)
        {
            int height = accumulator3D.GetLength(0);
            int width = accumulator3D.GetLength(1);
            int radiusRange = accumulator3D.GetLength(2);

            int[,,] suppressedAccumulator = new int[height, width, radiusRange];

            // iterate through accumulator avoiding the border
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int r = 1; r < radiusRange - 1; r++)
                    {

                        int currentVote = accumulator3D[y, x, r];

                        bool isLocalMax = true;

                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dr = -1; dr <= 1; dr++)
                                {
                                    if (accumulator3D[y + dy, x + dx, r + dr] > currentVote)
                                    {
                                        isLocalMax = false;
                                        break;
                                    }
                                }
                                if (!isLocalMax) break;
                            }
                            if (!isLocalMax) break;
                        }

                        // If it's a local maximum, keep it in the suppressed accumulator
                        if (isLocalMax)
                        {
                            suppressedAccumulator[y, x, r] = currentVote;
                        }
                    }
                }
            }

            return suppressedAccumulator;
        }






        private static List<(int x, int y, int radius)> findPeaks(int[,,] accumulator3D, int threshold)
        {
            int height = accumulator3D.GetLength(0);
            int width = accumulator3D.GetLength(1);
            int radiusRange = accumulator3D.GetLength(2);

            List<(int x, int y, int radius)> peaks = new List<(int x, int y, int radius)>();


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int r = 0; r < radiusRange; r++)
                    {
                        if (accumulator3D[y, x, r] >= threshold)
                        {

                            int radius = (int)(r); // radius index
                            peaks.Add((x, y, radius));
                        }
                    }
                }
            }

            return peaks;
        }


        private static int findMaxAccumulatorValue(int[,,] accumulator, int radiusRange, int imgHeight, int imgWidth)
        {
            int maxAccumulatorValue = 0;
            for (int r = 0; r < radiusRange; r++)
            {
                for (int y = 0; y < imgHeight; y++)
                {
                    for (int x = 0; x < imgWidth; x++)
                    {
                        if (accumulator[y, x, r] > maxAccumulatorValue)
                        {
                            maxAccumulatorValue = accumulator[y, x, r];
                        }
                    }
                }
            }
            return maxAccumulatorValue;
        }





    }
}
