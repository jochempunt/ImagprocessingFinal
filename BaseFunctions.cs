using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    public static class BaseFunctions
    {

        /// <summary>
        /// apply linear filtering of an input image, without clamping the result (including negative values)
        /// </summary>
        /// <param name="inputImage"></param>
        /// <param name="filter"></param>
        /// <param name="paddingFunction"></param>
        /// <returns>int [,] convolution result</returns>
        public static int[,] convolveImageSigned(byte[,] inputImage, float[,] filter, PaddingFunctions paddingFunction = PaddingFunctions.BorderExtension)
        {
            int filterSize = filter.GetLength(0); // assume filter is square
            int filterCenter = filterSize / 2;
            int imgWidth = inputImage.GetLength(1);
            int imgHeight = inputImage.GetLength(0);

            int[,] tempImage = new int[imgHeight, imgWidth];

            // Loop over each pixel in the input image
            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    float sum = 0.0f;
                    // apply filter to each pixel
                    for (int fX = -filterCenter; fX <= filterCenter; fX++)
                    {
                        for (int fY = -filterCenter; fY <= filterCenter; fY++)
                        {
                            //calculate pixel position in the image based on the filter kernel
                            int imgX = x + fX;
                            int imgY = y + fY;
                            sum += getPixelValueWithPadding(inputImage, imgY, imgX, paddingFunction) * filter[fY + filterCenter, fX + filterCenter];
                        }
                    }
                    tempImage[y, x] = (int)Math.Round(sum);
                }
            }
            return tempImage;
        }

        /*
        * convolveImage: apply linear filtering of an input image
        * input:   inputImage          single-channel (byte) image
        *          filter              linear kernel
        * output:                      single-channel (byte) image
        */


        /// <summary>
        /// apply linear filtering of an input image
        /// </summary>
        /// <param name="inputImage">single-channel (byte) image</param>
        /// <param name="filter">linear kernel</param>
        /// <param name="paddingFunction">optional padding function</param>
        /// <returns>single-channel Outputimage</returns>
        public static byte[,] convolveImage(byte[,] inputImage, float[,] filter, PaddingFunctions paddingFunction = PaddingFunctions.BorderExtension)
        {

            int imgWidth = inputImage.GetLength(1);
            int imgHeight = inputImage.GetLength(0);

            int[,] signedResult = convolveImageSigned(inputImage, filter, paddingFunction);

            byte[,] clampedResult = new byte[imgHeight, imgWidth];

            // Loop over each pixel in the input image
            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    //store the new value and make sure its in range
                    clampedResult[y, x] = (byte)Math.Min(Math.Max(signedResult[y, x], 0), 255);
                }
            }
            return clampedResult;
        }



        /// <summary>
        /// returns pixel value with specified padding applied for out-of-bounds coordinates
        /// </summary>
        /// <param name="image"> single-channel (byte) image</param>
        /// <param name="y">pixel coordinate Y</param>
        /// <param name="x">pixel coordinate Y</param>
        /// <param name="paddingType">defines the type of padding (BorderExtension, ZeroPadding, ReflectPadding)</param>
        /// <returns>Intensity value of the pixel (byte) , with padding adjustments</returns>
        public static byte getPixelValueWithPadding(byte[,] image, int y, int x, PaddingFunctions paddingType)
        {
            int width = image.GetLength(1);
            int height = image.GetLength(0);

            //if the image is inside of the borders, just return the normal pixel values, for performance reasons
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return image[y, x];
            }

            switch (paddingType)
            {
                case PaddingFunctions.BorderExtension:
                    // Clamp coordinates to the border of the image 
                    x = Math.Max(0, Math.Min(width - 1, x));
                    y = Math.Max(0, Math.Min(height - 1, y));
                    break;
                case PaddingFunctions.ZeroPadding:
                    // if goes outside of the borders, return zero
                    if (x < 0 | x >= width | y < 0 | y >= height)
                    {
                        return 0;
                    }
                    break;
                case PaddingFunctions.ReflectPadding:
                    // if outside of the borders set  the opposite reflected pixel intensity inside the image 
                    if (x < 0)
                    {
                        x = -x;
                    }
                    if (x >= width)
                    {
                        x = 2 * (width - 1) - x;
                    }

                    if (y < 0)
                    {
                        y = -y;
                    }
                    if (y >= height)
                    {
                        y = 2 * (height - 1) - y;
                    }
                    //security clamp to image borders
                    x = Math.Max(0, Math.Min(width - 1, x));
                    y = Math.Max(0, Math.Min(height - 1, y));
                    break;
            }
            return image[y, x];
        }


        /// <summary>
        /// converts a kernel from sbyte to float
        /// </summary>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static float[,] convertKernelToFloat(sbyte[,] kernel)
        {
            int width = kernel.GetLength(0);
            int height = kernel.GetLength(1);
            float[,] floatKernel = new float[width, height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    floatKernel[i, j] = (float)kernel[i, j];
                }
            }
            return floatKernel;
        }


        /// <summary>
        /// computes the logical AND of two binary images
        /// </summary>
        /// <param name="imageA"></param>
        /// <param name="imageB"></param>
        /// <returns>returns a byte[,] array representing the resulting binary image from AND operation</returns>
        public static byte[,] andImages(byte[,] imageA, byte[,] imageB)
        {
            if (!sameImageSizes(imageA, imageB))
            {
                throw new ArgumentException("Images need to have the same dimensions / size");
            }

            if (!isBinaryImage(imageA))
            {
                throw new ArgumentException("Image 1 is not a binary image");
            }

            if (!isBinaryImage(imageB))
            {
                throw new ArgumentException("Image 2 is not a binary image");
            }

            int width = imageA.GetLength(1);
            int height = imageA.GetLength(0);

            byte[,] unionImage = new byte[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (imageA[y, x] == 255 && imageB[y, x] == 255)
                    {
                        unionImage[y, x] = 255;
                    }
                    else
                    {
                        unionImage[y, x] = 0;
                    }
                }
            }
            return unionImage;
        }

     
        /// <summary>
        /// computes the logical OR of two binary images
        /// </summary>
        /// <param name="imageA"></param>
        /// <param name="imageB"></param>
        /// <returns>returns a byte[,] array representing the resulting binary image from the OR operation</returns>
        public static byte[,] orImages(byte[,] imageA, byte[,] imageB)
        {
            if (!sameImageSizes(imageA, imageB))
            {
                throw new ArgumentException("Images need to have the same dimensions / size");
            }

            if (!isBinaryImage(imageA))
            {
                throw new ArgumentException("Image 1 is not a binary image");
            }

            if (!isBinaryImage(imageB))
            {
                throw new ArgumentException("Image 2 is not a binary image");
            }

            int width = imageA.GetLength(1);
            int height = imageA.GetLength(0);

            byte[,] intersectionImage = new byte[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (imageA[y, x] == 255 || imageB[y, x] == 255)
                    {
                        intersectionImage[y, x] = 255;
                    }
                }
            }
            return intersectionImage;
        }

        

        /// <summary>
        /// checks if the given image is a binary image where pixel values are either 0 or 255.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static bool isBinaryImage(byte[,] image)
        {
            int height = image.GetLength(0);
            int width = image.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte pixelValue = image[y, x];

                    if (pixelValue != 0 && pixelValue != 255)
                    {
                        return false;
                    }
                }
            }

            return true; // if pixels are  0 or 255 its a binary image
        }

        /// <summary>
        /// compares the dimensions of two images to ensure they are the same size.
        /// </summary>
        /// <param name="imageA"></param>
        /// <param name="imageB"></param>
        /// <returns>returns true if the dimensions of both images are the same, false otherwise</returns>
        public static bool sameImageSizes(byte[,] imageA, byte[,] imageB)
        {
            int height1 = imageA.GetLength(0);
            int width1 = imageA.GetLength(1);
            int height2 = imageB.GetLength(0);
            int width2 = imageB.GetLength(1);

            if (height1 != height2 || width1 != width2)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}

