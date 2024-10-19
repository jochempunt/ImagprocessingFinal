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
    }
}
