using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    public class EdgeDetector
    {
        private static sbyte[,] sobelHorizontal =             {{ -1,  0,  1},
                                                    { -2,  0,  2},
                                                    { -1,  0,  1}};

        private static sbyte[,] sobelVertical =                {{ -1, -2, -1},
                                                    {  0,  0,  0},
                                                    {  1,  2,  1}};

        /*
        * edgeMagnitude: calculate the image derivative of an input image and a provided edge kernel
        * input:   inputImage          single-channel (byte) image
        *          horizontalKernel    horizontal edge kernel
        *          virticalKernel      vertical edge kernel
        * output:                      single-channel (byte) image
        */


        /// <summary>
        /// calculate the image derivative of an input image (using sobel kernels)
        /// </summary>
        /// <param name="inputImage"></param>
        /// <returns>edge strength image (greyscale)</returns>
        public static byte[,] getEdgeMagnitude(byte[,] inputImage)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            int imgWidth = inputImage.GetLength(1);
            int imgHeight = inputImage.GetLength(0);

            // calculate gradients
            // convolve with kernels, but dont clamp values yet, so the negative values are also taken into consideration
            int[,] gradientImgX = BaseFunctions.convolveImageSigned(inputImage, BaseFunctions.convertKernelToFloat(sobelHorizontal), PaddingFunctions.ReflectPadding);
            int[,] gradientImgY = BaseFunctions.convolveImageSigned(inputImage, BaseFunctions.convertKernelToFloat(sobelVertical), PaddingFunctions.ReflectPadding);

            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    // get gradient values for both directions
                    int gX = gradientImgX[y, x];
                    int gY = gradientImgY[y, x];

                    // calculate  magnitude of  gradient
                    int magnitude = (int)Math.Sqrt(gX * gX + gY * gY);

                    // clamp  result to fit in byte range
                    tempImage[y, x] = (byte)Math.Min(255, magnitude);
                }
            }
            return tempImage;
        }


        //....................... canny edge detection ........................


        /// <summary>
        /// detects edges in an image using the Canny edge detection algorithm
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="lowThreshold">lower threshold for edge hysteresis (weak edges)</param>
        /// <param name="highThreshold">upper threshold for edge hysteresis (strong edges</param>
        /// <param name="specificSigma">sigma value for Gaussian blur</param>
        /// <param name="kSize">kernel size for gauss (optional)</param>
        /// <returns>byte[,] image with detected edges</returns>
        public static byte[,] detectEdgesCanny(byte[,] inputImage, byte lowThreshold, byte highThreshold, float specificSigma,byte kSize = 3)
        {
            int height = inputImage.GetLength(0);
            int width = inputImage.GetLength(1);

            byte[,] blurredImage = new byte[height, width];
            blurredImage = Preprocessor.applyGaussianFilter(inputImage,specificSigma,kSize);

            // calculate image gradients
            int[,] gradientX = BaseFunctions.convolveImageSigned(blurredImage, BaseFunctions.convertKernelToFloat(sobelHorizontal), PaddingFunctions.ReflectPadding);
            int[,] gradientY = BaseFunctions.convolveImageSigned(blurredImage, BaseFunctions.convertKernelToFloat(sobelVertical), PaddingFunctions.ReflectPadding);

            // calculate edge magnitudes
            byte[,] edgeMagnitudes = new byte[height, width];

            // calculate edge orientations using octants
            Octant[,] edgeOrientations = new Octant[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // get gradient values for both directions
                    int gX = gradientX[y, x];
                    int gY = gradientY[y, x];
                    // calculate  magnitude of  gradient
                    int magnitude = (int)Math.Sqrt(gX * gX + gY * gY);
                    // clamp  result to fit in byte range
                    edgeMagnitudes[y, x] = (byte)Math.Min(255, magnitude);

                    //get the edge orientations using octants
                    edgeOrientations[y, x] = getOctant(gY, gX);
                }
            }

            byte[,] supressedImage = nonMaxSuppression(edgeMagnitudes, edgeOrientations);
            //return supressedImage;
            return hysteresisThresholding(edgeMagnitudes, supressedImage, lowThreshold, highThreshold);

        }

        /*
         * nonMaxSuppression: suppresses non-maximum edge magnitudes based on edge orientations
         * input:   edgeMagnitudes     byte[,] array of edge magnitudes
         *          edgeOrientations   array of octant-based edge orientations
         * output:                     byte[,] image with non-maximum edges suppressed
         */
        private static byte[,] nonMaxSuppression(byte[,] edgeMagnitudes, Octant[,] edgeOrientations)
        {
            int height = edgeMagnitudes.GetLength(0);
            int width = edgeMagnitudes.GetLength(1);
            byte[,] supressedImage = new byte[height, width];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    byte currMagnitude = edgeMagnitudes[y, x];

                    if (currMagnitude == 0)
                        continue; // Skip non-edge pixels
                                  // get neighboring pixels in the direction of the octant orientation
                    (sbyte[] neighbour1, sbyte[] neighbour2) = getNeigbourForOctant(edgeOrientations[y, x]);

                    byte neighbour1Magnitude = edgeMagnitudes[y + neighbour1[1], x + neighbour1[0]];
                    byte neighbour2Magnitude = edgeMagnitudes[y + neighbour2[1], x + neighbour2[0]];

                    if (currMagnitude >= neighbour1Magnitude && currMagnitude >= neighbour2Magnitude)
                    {
                        supressedImage[y, x] = currMagnitude;
                    }
                    else
                    {
                        supressedImage[y, x] = 0; // suppress non-local maximums
                    }

                }
            }
            return supressedImage;
        }

        /*
         * hysteresisThresholding: applies edge tracking by hysteresis to keep strong edges and trace weak edges connected to them
         * input:   magnitudeImage     byte[,] array of edge magnitudes
         *          suppressedImage    byte[,] array after non-maximum suppression
         *          lowThreshold       lower threshold for weak edges
         *          highThreshold      upper threshold for strong edges
         * output:                     byte[,] image with final detected edges
         */
        private static byte[,] hysteresisThresholding(byte[,] magnitudeImage, byte[,] suppressedImage, byte lowThreshold, byte highThreshold)
        {

            if (lowThreshold <= 0)
            {
                throw new ArgumentException("Low threshold must be greater than zero.");
            }
            if (highThreshold <= 0)
            {
                throw new ArgumentException("High threshold must be greater than zero.");
            }
            if (lowThreshold >= highThreshold)
            {
                throw new ArgumentException("Low threshold must be less than high threshold.");
            }

            int height = suppressedImage.GetLength(0);
            int width = suppressedImage.GetLength(1);
            bool[,] processed = new bool[height, width];
            byte[,] finalEdges = new byte[height, width];

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (suppressedImage[y, x] >= highThreshold && !processed[y, x])
                    {
                        // follow connected weak edges recursivly
                        //traceEdges(x, y, suppressedImage, processed, finalEdges, lowThreshold, highThreshold);
                        traceEdgesIteratively(x, y, suppressedImage, processed, finalEdges, lowThreshold);
                    }
                }
            }
            return finalEdges;
        }


        private static void traceEdgesIteratively(int x, int y, byte[,] suppressedImage, bool[,] processed, byte[,] finalEdges, byte lowThreshold)
        {
            // directions for neighbors (8)
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            // Stack for pixels to process
            Stack<(int x, int y)> stack = new Stack<(int x, int y)>();
            stack.Push((x, y));

            while (stack.Count > 0)
            {
                (int currentX, int currentY) = stack.Pop();

                // Mark current pixel as part of final edges
                finalEdges[currentY, currentX] = 255;
                processed[currentY, currentX] = true;

                // Check all neighboring pixels
                for (int i = 0; i < 8; i++)
                {
                    int newX = currentX + dx[i];
                    int newY = currentY + dy[i];

                    // Check if the neighbor is within bounds and not processed
                    if (newX >= 0 && newX < suppressedImage.GetLength(1) && newY >= 0 && newY < suppressedImage.GetLength(0) && !processed[newY, newX])
                    {
                        byte neighborMagnitude = suppressedImage[newY, newX];

                        // if the neighbor is a weak edge, add it to the stack
                        if (neighborMagnitude >= lowThreshold)
                        {
                            stack.Push((newX, newY));
                        }
                    }
                }
            }
        }




        /*
         * getNeigbourForOctant: retrieves offsets for neighboring pixels based on the given octant
         * input:   octant             the octant direction for edge orientation
         * output:                    (sbyte[] Offset1, sbyte[] Offset2) offsets for neighboring pixels
         */
        private static (sbyte[] Offset1, sbyte[] Offset2) getNeigbourForOctant(Octant octant)
        {
            switch (octant)
            {
                case Octant.Octant0:  // Diagonal (45°)
                    return (new sbyte[] { 1, -1 }, new sbyte[] { -1, 1 });
                case Octant.Octant1:  // Vertical (90°)
                    return (new sbyte[] { 0, 1 }, new sbyte[] { 0, -1 });
                case Octant.Octant2:  // Diagonal (135°)
                    return (new sbyte[] { -1, -1 }, new sbyte[] { 1, 1 });
                case Octant.Octant3:  // Horizontal (180°)
                    return (new sbyte[] { -1, 0 }, new sbyte[] { 1, 0 });
                default:
                    return (new sbyte[] { 0, 0 }, new sbyte[] { 0, 0 });
            }
        }

        /*
         * getOctant: determines the octant based on gradient values for edge orientation
         * input:   gY                 gradient in the Y direction
         *          gX                 gradient in the X direction
         * output:                    Octant representing the edge orientation
         */
        private static Octant getOctant(int gY, int gX)
        {
            if (gX == 0 && gY == 0)
            {
                return Octant.None;
            }

            if (gX >= 0 && gY < 0) // quadrant 0 (1-90°)
            {
                if (Math.Abs(gX) > Math.Abs(gY))
                {
                    return Octant.Octant0; // 45°
                }
                else
                {
                    return Octant.Octant1; //90°
                }
            }
            else if (gX < 0 && gY <= 0) // quadrant 1 (91-180°)
            {
                if (Math.Abs(gX) < Math.Abs(gY))
                {
                    return Octant.Octant2; //135°
                }
                else
                {
                    return Octant.Octant3; // 180°
                }
            }
            else if (gX <= 0 && gY > 0) //quadrant 2 (181 - 270°)
            {
                if (Math.Abs(gX) > Math.Abs(gY))
                {
                    return Octant.Octant0;
                }
                else
                {
                    return Octant.Octant1;
                }
            }
            else if (gX > 0 && gY >= 0) // quadrant 3 (271-360°)
            {
                if (Math.Abs(gX) < Math.Abs(gY))
                {
                    return Octant.Octant2;
                }
                else
                {
                    return Octant.Octant3;
                }
            }
            return Octant.None;
        }

        private enum Octant : byte
        {
            Octant0 = 0,  // Diagonal   (45°)
            Octant1 = 1,  // vertical   (90°)
            Octant2 = 2,  // diagonal   (135°)
            Octant3 = 3,  // horizontal (180°)
            None = 4,
        }
    }
}
