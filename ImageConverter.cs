using System;
using System.Collections.Generic;
using System.Drawing;


namespace INFOIBV
{
    public static class ImageConverter
    {
        public static byte[,] BitmapToGrayscale(Bitmap input)
        {
            int width = input.Width;
            int height = input.Height;
            byte[,] output = new byte[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = input.GetPixel(x, y);
                    output[y, x] = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                }
            }
            return output;
        }

        public static Color[,] BitmapToColor(Bitmap input)
        {
            int width = input.Width;
            int height = input.Height;
            Color[,] output = new Color[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = input.GetPixel(x, y);
                    output[y, x] = pixel;
                }
            }
            return output;
        }

        public static Bitmap GrayscaleToBitmap(byte[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            Bitmap output = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte value = input[y, x];
                    output.SetPixel(x, y, Color.FromArgb(value, value, value));
                }
            }
            return output;
        }


        public static Bitmap ColorToBitmap(Color[,] input)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            Bitmap output = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color value = input[y, x];
                    output.SetPixel(x, y, value);
                }
            }
            return output;
        }

        private const double RED_LUMA_COEFFICIENT = 0.299;
        private const double GREEN_LUMA_COEFFICIENT = 0.587;
        private const double BLUE_LUMA_COEFFICIENT = 0.114;

        /// <summary>
        /// Converts Color[,] to grayscale byte[,] for processing
        /// </summary>
        public static byte[,] ToGrayscale(Color[,] colorImage)
        {
            int height = colorImage.GetLength(0);
            int width = colorImage.GetLength(1);
            byte[,] grayscale = new byte[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = colorImage[y, x];
                    grayscale[y, x] = (byte)(
                        RED_LUMA_COEFFICIENT * pixel.R +
                        GREEN_LUMA_COEFFICIENT * pixel.G +
                        BLUE_LUMA_COEFFICIENT * pixel.B
                    );
                }
            }
            return grayscale;
        }

        /// <summary>
        /// Converts grayscale byte[,] back to Color[,]
        /// </summary>
        public static Color[,] ToColorImage(byte[,] grayscale)
        {
            int height = grayscale.GetLength(0);
            int width = grayscale.GetLength(1);
            Color[,] colorImage = new Color[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte value = grayscale[y, x];
                    colorImage[y, x] = Color.FromArgb(value, value, value);
                }
            }
            return colorImage;
        }
    }
}
