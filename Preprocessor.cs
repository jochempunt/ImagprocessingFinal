using System;
using System.Collections.Generic;

namespace INFOIBV
{
    /// <summary>
    /// here you can find standard preprocessing functions like adjusting contrast, blurring, median filter or thresholding
    /// </summary>
    public class Preprocessor
    {

        #region Intensity Transformations
        /// <summary>
        /// Adjust the contrast of a grayscale image.
        /// </summary>
        /// <param name="image">Factor to reduce contrast (0 < contrastFactor < 1)</param>
        /// <param name="contrastFactor"></param>
        /// <returns>2D byte array with reduced contrast</returns>
        public static byte[,] reduceContrast(byte[,] image, double contrastFactor)
        {
            int width = image.GetLength(0);
            int height = image.GetLength(1);
            byte[,] result = new byte[width, height];
            byte midGray = 128;  // Mid gray value to shift towards

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    // adjust the pixel value by reducing contrast

                    int adjustedValue = (int)((image[i, j] - midGray) * contrastFactor + midGray);

                    // clamp the result to valid byte range (0-255)
                    result[i, j] = (byte)Math.Max(0, Math.Min(255, adjustedValue));
                }
            }

            return result;
        }









        /// <summary>
        /// invert a single channel (grayscale) image
        /// </summary>
        /// <param name="input">inputImage</param>
        /// <returns>inverted image</returns>
        /// 
        public static byte[,] InvertImage(byte[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            byte[,] output = new byte[height, width];

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    output[h, w] = (byte)(255 - input[h, w]);
                }
            }
            return output;
        }




        /// <summary>
        /// create an image with the full range of intensity values used
        /// </summary>
        /// <param name="inputImage">inputImage</param>
        /// <returns>image with adjusted contrast</returns>
        public static byte[,] adjustContrast(byte[,] inputImage)
        {

            int height = inputImage.GetLength(0);
            int width = inputImage.GetLength(1);
            // create temporary grayscale image
            byte[,] tempImage = new byte[height, width];


            //First we need to find the max and min intensity
            int maxVal = 0;
            int minVal = 255;
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                {
                    byte pixelVal = inputImage[h, w];
                    if (pixelVal < minVal)
                        minVal = pixelVal;
                    if (pixelVal > maxVal)
                        maxVal = pixelVal;
                }
            // We use the formula (Val - minVal) * 255 / (maxVal - minVal)) to caclulate the appropriate value
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                {
                    byte Val = inputImage[h, w];
                    if (maxVal != minVal)
                        tempImage[h, w] = (byte)((Val - minVal) * 255 / (maxVal - minVal)); // no worries about Val - minVal becoming less than zero. Data type Byte will clamp it automatically
                    else
                        tempImage[h, w] = Val;
                }
            return tempImage;
        }


        /// <summary>
        /// Equalize the histogram of the pciture
        /// </summary>
        /// <param name="image"></param>
        /// <returns>byte[,] image with its histogram equalized.</returns>
        public static byte[,] HistogramEqual(byte[,] image)
        {
            byte[,] temp = image;
            Preprocessor.adjustContrast(temp);

            int height = temp.GetLength(0);
            int width = temp.GetLength(1);

            int[] histogram = new int[256];
            //create the histogram
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                {
                    histogram[temp[h, w]] += 1;
                }

            int[] cumulative = new int[256];

            //fill the cumulative
            for (int i = 1; i < 256; i++)
            {
                cumulative[i] = cumulative[i - 1] + histogram[i];
            }

            int min = Array.Find(cumulative, x => x != 0); // minimum non-zero value in cumulative.
            int totalPix = width * height;
            byte[] equalHist = new byte[256];

            byte[,] equalImage = new byte[height, width];
            for (int i = 0; i < 256; i++)
            {
                equalHist[i] = (byte)(cumulative[i] * 255 / totalPix);
            }

            //create the equalized image.
            for (int h = 0; h < height; h++)
                for (int w = 0; w < width; w++)
                {
                    equalImage[h, w] = equalHist[temp[h, w]];
                }

            return equalImage;
        }



        /// <summary>
        /// binary threshold a grayscale image
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="threshold"></param>
        /// <returns>single-channel (byte) image with on/off values</returns>
        public static byte[,] thresholdImage(byte[,] inputImage, byte threshold)
        {
            // Get the dimensions of the input image
            int height = inputImage.GetLength(0);
            int width = inputImage.GetLength(1);

            // Create a new image to store the thresholded result
            byte[,] tempImage = new byte[height, width];

            // Iterate through each pixel in the input image
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    // Get the pixel value from the input image
                    byte Val = inputImage[h, w];

                    // Apply the thresholding operation
                    if (Val >= threshold)
                    {
                        tempImage[h, w] = 255; // white
                    }
                    else
                    {
                        tempImage[h, w] = 0; //  black
                    }
                }
            }

            return tempImage;
        }
        #endregion

        #region Spatial Filtering
        /// <summary>
        /// apply a gaussian filter to the image (blurring)
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="filterSigma"></param>
        /// <param name="kSize"> kernel size</param>
        /// <returns>blurred image</returns>
        public static byte[,] applyGaussianFilter(byte[,] inputImage, float filterSigma, byte kSize = 3)
        {
            var preProc = new Preprocessor();
            float[,] gFilter = preProc.createGaussianKernel(kSize, filterSigma);
            return BaseFunctions.convolveImage(inputImage, gFilter);
        }
        /*
        * medianFilter: apply median filtering on an input image with a kernel of specified size
        * input:   inputImage          single-channel (byte) image
        *          size                length/width of the median filter kernel
        * output:                      single-channel (byte) image
        */
        public static byte[,] applyMedianFilter(byte[,] inputImage, byte size)
        {
            // Ensure size is odd for the median filter
            if (size % 2 == 0) throw new ArgumentException("Size must be a odd number");

            int height = inputImage.GetLength(0);
            int width = inputImage.GetLength(1);
            int radius = size / 2;

            // Create a temporary image to store the filtered result
            byte[,] tempImage = new byte[height, width];

            // Helper function to get the median of a list of byte values
            byte GetMedian(List<byte> vals)
            {
                vals.Sort();
                return vals[vals.Count / 2];
            }

            // use median filter on each pixel in the image
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    //get all the neighbouring pixels
                    List<byte> kernel = new List<byte>();

                    for (int hh = -radius; hh <= radius; hh++)
                    {
                        for (int ww = -radius; ww <= radius; ww++)
                        {
                            //neighbours in different directions
                            int nh = h + hh;
                            int nw = w + ww;

                            // boundary check
                            if (nh >= 0 && nh < height && nw >= 0 && nw < width)
                            {
                                kernel.Add(inputImage[nh, nw]);
                            }
                        }
                    }

                    tempImage[h, w] = GetMedian(kernel);
                }
            }
            return tempImage;
        }
        #endregion

        #region helper functions
        private float[,] createGaussianKernel(byte size, float sigma)
        {
            // security check, if kernel size is really odd or not
            if (size % 2 == 0)
            {
                throw new ArgumentException("Size must be an odd number.");
            }
            Console.WriteLine("Gaussian filter kernel size: " + size);

            float[,] filter = new float[size, size];
            int center = size / 2;
            float sigma2 = sigma * sigma;


            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // calculate offset, since the center is in the middle of the matrix
                    int offsetX = x - center;
                    int offsetY = y - center;

                    float exp = (float)-((offsetX * offsetX + offsetY * offsetY) / (2 * sigma2));
                    filter[x, y] = (float)Math.Exp(exp);
                }
            }

            // normalize the filter
            filter = normaliseFilter(filter);
            return filter;
        }

        /// <summary>
        /// Normalizes a square filter kernel so that the sum of all its values equals 1
        /// </summary>
        /// <param name="filter"> un-normalized filter kernel,should be a square matrix </param>
        /// <returns>normalised 2D kernel</returns>
        private float[,] normaliseFilter(float[,] filter)
        {
            int size = filter.GetLength(0);
            float sum = 0.0f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    sum += filter[x, y];
                }
            }

            // avoid division by zero
            if (sum == 0)
            {
                throw new InvalidOperationException("Sum of filter values is 0, cant normalize");
            }

            //normalise matrix
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    filter[x, y] /= sum;
                }
            }
            return filter;
        }
        #endregion
    }
}
