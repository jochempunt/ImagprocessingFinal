using System;

namespace INFOIBV
{
    /// <summary>
    /// Provides morphological operations for image processing
    /// </summary>
    public class Morphology
    {
        // Enums for better type safety and readability
        public enum ElementShape { Square, Plus }
        public enum ImageType { Binary, Grayscale }

        #region Public Morphology Methods


        /// <summary>
        /// Performs morphological dilation on an input image.
        /// </summary>
        /// <param name="image">Input image as 2D byte array</param>
        /// <param name="shape">Shape of the structuring element (Square or Plus)</param>
        /// <param name="kernelSize">Size of the structuring element (must be odd)</param>
        /// <param name="imageType">Type of image processing (Binary or Grayscale)</param>
        /// <param name="mask">Optional binary mask for geodesic operations. 
        /// When provided, dilation is constrained to mask's foreground regions (255 values)</param>
        /// <returns>Dilated image as 2D byte array</returns>
        /// <exception cref="ArgumentException">Thrown when kernel size is even or inputs are invalid</exception>
        public static byte[,] Dilate(byte[,] image, ElementShape shape = ElementShape.Square,
            int kernelSize = 3, ImageType imageType = ImageType.Binary, byte[,] mask = null)
        {
            ValidateInputs(image, kernelSize, mask, imageType);
            var structuringElement = CreateStructuringElement(shape, kernelSize, imageType);
            return DilateImage(image, structuringElement, imageType, mask);
        }


        /// <summary>
        /// Performs morphological erosion on an input image.
        /// </summary>
        /// <param name="image">Input image as 2D byte array</param>
        /// <param name="shape">Shape of the structuring element (Square or Plus)</param>
        /// <param name="kernelSize">Size of the structuring element (must be odd)</param>
        /// <param name="imageType">Type of image processing (Binary or Grayscale)</param>
        /// <param name="mask">Optional binary mask for geodesic operations. 
        /// When provided, erosion is constrained to mask's foreground regions (255 values)</param>
        /// <returns>Eroded image as 2D byte array</returns>
        /// <exception cref="ArgumentException">Thrown when kernel size is even or inputs are invalid</exception>
        public static byte[,] Erode(byte[,] image, ElementShape shape = ElementShape.Square,
            int kernelSize = 3, ImageType imageType = ImageType.Grayscale, byte[,] mask = null)
        {
            ValidateInputs(image, kernelSize, mask, imageType);
            var structuringElement = CreateStructuringElement(shape, kernelSize, imageType);
            return ErodeImage(image, structuringElement, imageType, mask);
        }

        /// <summary>
        /// Performs morphological opening (erosion followed by dilation).
        /// Removes small foreground details while preserving the overall shape of larger features.
        /// </summary>
        /// <param name="image">Input image as 2D byte array</param>
        /// <param name="shape">Shape of the structuring element (Square or Plus)</param>
        /// <param name="kernelSize">Size of the structuring element (must be odd)</param>
        /// <param name="imageType">Type of image processing (Binary or Grayscale)</param>
        /// <returns>Opened image as 2D byte array</returns>
        /// <exception cref="ArgumentException">Thrown when kernel size is even or inputs are invalid</exception>
        public static byte[,] Open(byte[,] image, ElementShape shape = ElementShape.Square,
            int kernelSize = 3, ImageType imageType = ImageType.Grayscale)
        {
            ValidateInputs(image, kernelSize, null, imageType);
            var structuringElement = CreateStructuringElement(shape, kernelSize, imageType);
            var eroded = ErodeImage(image, structuringElement, imageType);
            return DilateImage(eroded, structuringElement, imageType);
        }

        /// <summary>
        /// Performs morphological closing (dilation followed by erosion).
        /// Fills small holes and gaps in foreground regions while preserving the overall shape.
        /// </summary>
        /// <param name="image">Input image as 2D byte array</param>
        /// <param name="shape">Shape of the structuring element (Square or Plus)</param>
        /// <param name="kernelSize">Size of the structuring element (must be odd)</param>
        /// <param name="imageType">Type of image processing (Binary or Grayscale)</param>
        /// <returns>Closed image as 2D byte array</returns>
        /// <exception cref="ArgumentException">Thrown when kernel size is even or inputs are invalid</exception>
        public static byte[,] Close(byte[,] image, ElementShape shape = ElementShape.Square,
            int kernelSize = 3, ImageType imageType = ImageType.Grayscale)
        {
            ValidateInputs(image, kernelSize, null, imageType);
            var structuringElement = CreateStructuringElement(shape, kernelSize, imageType);
            var dilated = DilateImage(image, structuringElement, imageType);
            return ErodeImage(dilated, structuringElement, imageType);
        }


        #endregion

        #region Private Methods

        private static void ValidateInputs(byte[,] image, int kernelSize, byte[,] mask, ImageType imageType)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (kernelSize < 3 || kernelSize % 2 == 0)
                throw new ArgumentException("Kernel size must be odd and >= 3", nameof(kernelSize));

            if (imageType == ImageType.Binary && !BaseFunctions.isBinaryImage(image))
                throw new ArgumentException("Image must be binary for binary operations");

            if (mask != null)
            {
                if (!BaseFunctions.sameImageSizes(image, mask))
                    throw new ArgumentException("Mask must be the same size as the image");

                if (imageType == ImageType.Binary && !BaseFunctions.isBinaryImage(mask))
                    throw new ArgumentException("Mask must be binary for binary operations");
            }
        }

        private static int[,] CreateStructuringElement(ElementShape shape, int size, ImageType imageType)
        {
            switch (shape)
            {
                case ElementShape.Plus:
                    return CreatePlusElement(size, imageType);
                case ElementShape.Square:
                    return CreateSquareElement(size, imageType);
                default:
                    throw new ArgumentException("Invalid element shape");
            }
        }

        private static int[,] CreateSquareElement(int size, ImageType imageType)
        {
            var square = new int[size, size];
            for (int row = 0; row < size; row++)
                for (int col = 0; col < size; col++)
                    square[row, col] = imageType == ImageType.Grayscale ? 0 : 1;
            return square;
        }

        private static int[,] CreatePlusElement(int size, ImageType imageType)
        {
            var basePlus = new int[3, 3]
            {
                {0,1,0},
                {1,1,1},
                {0,1,0}
            };

            var result = DilatePlusToSize(basePlus, size);

            if (imageType == ImageType.Grayscale)
            {
                for (int i = 0; i < result.GetLength(0); i++)
                    for (int j = 0; j < result.GetLength(1); j++)
                        result[i, j] = result[i, j] == 0 ? -1 : 0;
            }

            return result;
        }

        private static int[,] DilatePlusToSize(int[,] basePlus, int targetSize)
        {
            var currentSize = 3;
            var current = basePlus;

            while (currentSize < targetSize)
            {
                current = DilatePlus(current, currentSize);
                currentSize += 2;
            }

            return current;
        }

        private static int[,] DilatePlus(int[,] element, int currentSize)
        {
            var newSize = currentSize + 2;
            var padded = new int[newSize, newSize];
            var result = new int[newSize, newSize];
            var center = element.GetLength(0) / 2;

            // Pad the current element
            for (int i = 0; i < currentSize; i++)
                for (int j = 0; j < currentSize; j++)
                    padded[i + 1, j + 1] = element[i, j];

            // Perform dilation
            for (int i = 0; i < newSize; i++)
                for (int j = 0; j < newSize; j++)
                    result[i, j] = ComputeDilationAtPosition(padded, i, j, element, center);

            return result;
        }

        private static int ComputeDilationAtPosition(int[,] padded, int row, int col, int[,] kernel, int center)
        {
            for (int i = 0; i < kernel.GetLength(0); i++)
                for (int j = 0; j < kernel.GetLength(1); j++)
                {
                    var y = row + i - center;
                    var x = col + j - center;

                    if (IsInBounds(padded, y, x) && padded[y, x] == 1 && kernel[i, j] == 1)
                        return 1;
                }
            return 0;
        }

        private static bool IsInBounds<T>(T[,] array, int row, int col)
        {
            return row >= 0 && row < array.GetLength(0) &&
                   col >= 0 && col < array.GetLength(1);
        }

        private static byte[,] DilateImage(byte[,] image, int[,] structElement, ImageType imageType, byte[,] mask = null)
        {
            var height = image.GetLength(0);
            var width = image.GetLength(1);
            var result = new byte[height, width];
            var kernelSize = structElement.GetLength(0);
            var center = kernelSize / 2;

            for (int row = 0; row < height; row++)
                for (int col = 0; col < width; col++)
                    result[row, col] = ComputeDilation(image, structElement, row, col, center, imageType);

            if (mask != null)
                ApplyMask(result, mask, (a, b) => (byte)Math.Min(a, b));

            return result;
        }

        private static byte[,] ErodeImage(byte[,] image, int[,] structElement, ImageType imageType, byte[,] mask = null)
        {
            var height = image.GetLength(0);
            var width = image.GetLength(1);
            var result = new byte[height, width];
            var kernelSize = structElement.GetLength(0);
            var center = kernelSize / 2;

            for (int row = 0; row < height; row++)
                for (int col = 0; col < width; col++)
                    result[row, col] = ComputeErosion(image, structElement, row, col, center, imageType);

            if (mask != null)
                ApplyMask(result, mask, (a, b) => (byte)Math.Max(a, b));

            return result;
        }

        private static void ApplyMask(byte[,] result, byte[,] mask, Func<byte, byte, byte> operation)
        {
            for (int i = 0; i < result.GetLength(0); i++)
                for (int j = 0; j < result.GetLength(1); j++)
                    result[i, j] = operation(result[i, j], mask[i, j]);
        }

        private static byte ComputeDilation(byte[,] image, int[,] structElement, int row, int col, int center, ImageType imageType)
        {
            if (imageType == ImageType.Binary)
                return ComputeBinaryDilation(image, structElement, row, col, center);
            return ComputeGrayscaleDilation(image, structElement, row, col, center);
        }

        private static byte ComputeErosion(byte[,] image, int[,] structElement, int row, int col, int center, ImageType imageType)
        {
            if (imageType == ImageType.Binary)
                return ComputeBinaryErosion(image, structElement, row, col, center);
            return ComputeGrayscaleErosion(image, structElement, row, col, center);
        }

        private static byte ComputeBinaryDilation(byte[,] image, int[,] structElement, int row, int col, int center)
        {
            for (int i = 0; i < structElement.GetLength(0); i++)
                for (int j = 0; j < structElement.GetLength(1); j++)
                {
                    var y = row + i - center;
                    var x = col + j - center;

                    if (IsInBounds(image, y, x) && structElement[i, j] == 1 && image[y, x] == 255)
                        return 255;
                }
            return 0;
        }

        private static byte ComputeBinaryErosion(byte[,] image, int[,] structElement, int row, int col, int center)
        {
            for (int i = 0; i < structElement.GetLength(0); i++)
                for (int j = 0; j < structElement.GetLength(1); j++)
                {
                    var y = row + i - center;
                    var x = col + j - center;

                    if (IsInBounds(image, y, x) && structElement[i, j] == 1 && image[y, x] == 0)
                        return 0;
                }
            return 255;
        }

        private static byte ComputeGrayscaleDilation(byte[,] image, int[,] structElement, int row, int col, int center)
        {
            byte maxValue = 0;
            for (int i = 0; i < structElement.GetLength(0); i++)
                for (int j = 0; j < structElement.GetLength(1); j++)
                {
                    var y = row + i - center;
                    var x = col + j - center;

                    if (IsInBounds(image, y, x) && structElement[i, j] != -1)
                    {
                        var newValue = (byte)Math.Min(image[y, x] + structElement[i, j], 255);
                        maxValue = Math.Max(maxValue, newValue);
                    }
                }
            return maxValue;
        }

        private static byte ComputeGrayscaleErosion(byte[,] image, int[,] structElement, int row, int col, int center)
        {
            byte minValue = 255;
            for (int i = 0; i < structElement.GetLength(0); i++)
                for (int j = 0; j < structElement.GetLength(1); j++)
                {
                    var y = row + i - center;
                    var x = col + j - center;

                    if (IsInBounds(image, y, x) && structElement[i, j] != -1)
                    {
                        var newValue = (byte)Math.Max(image[y, x] - structElement[i, j], 0);
                        minValue = Math.Min(minValue, newValue);
                    }
                }
            return minValue;
        }

        #endregion
    }
}