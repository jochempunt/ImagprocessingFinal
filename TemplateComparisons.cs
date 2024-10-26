using INFOIBV;
using System;
using System.Collections.Generic;

public class TemplateComparisons
{
  
    private static byte[,] CreateMR(int scale)
    {
        byte field1 = (byte)(1 * scale);
        byte field2 = (byte)(Math.Sqrt(2) * scale);
        byte field3 = (byte)(1 * scale);
        byte field4 = (byte)(Math.Sqrt(2) * scale);

        return new byte[,]
        {
            {0,      0,      0     },
            {0,      0,      field1},
            {field4, field3, field2}
        };
    }

    private static byte[,] CreateML(int scale)
    {
        byte field1 = (byte)(1 * scale);
        byte field2 = (byte)(Math.Sqrt(2) * scale);
        byte field3 = (byte)(1 * scale);
        byte field4 = (byte)(Math.Sqrt(2) * scale);

        return new byte[,]
        {
            {field2, field3, field4},
            {field1, 0,      0     },
            {0,      0,      0     }
        };
    }

    private static byte[,] DistanceTransform(byte[,] input)
    {
        int Height = input.GetLength(0);
        int Width = input.GetLength(1);

        byte[,] result = new byte[Height, Width];
        byte[,] Mr = CreateMR(1);
        byte[,] Ml = CreateML(1);

        // Initialize distance transform (255 is foreground)
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                result[row, col] = (byte)(input[row, col] == 255 ? 0 : 255);
            }
        }

        // Apply forward and backward passes
        result = L_R_Mask(result, Ml);

        result = R_L_Mask(result, Mr);

        return result;
    }


    private static int countForegroundPixel(byte[,] input)
    {
        int foregroundCount = 0; // Initialize a counter for foreground pixels

        // Loop through each element in the 2D byte array
        for (int row = 0; row < input.GetLength(0); row++)
        {
            for (int col = 0; col < input.GetLength(1); col++)
            {
                // Check if the current pixel is a foreground pixel
                if (input[row, col] == 255)
                {
                    foregroundCount++; // Increment the counter
                }
            }
        }

        return foregroundCount; // Return the total count of foreground pixels
    }

    private static byte[,] L_R_Mask(byte[,] input, byte[,] Mask)
    {
        byte[,] temp = new byte[input.GetLength(0), input.GetLength(1)];
        Array.Copy(input, temp, input.Length);

        for (int row = 1; row < input.GetLength(0) - 1; row++)
        {
            for (int col = 1; col < input.GetLength(1) - 1; col++)
            {
                byte current = temp[row, col];
                byte d1 = (byte)(Mask[1, 0] + temp[row, col - 1]);
                byte d2 = (byte)(Mask[0, 0] + temp[row - 1, col - 1]);
                byte d3 = (byte)(Mask[0, 1] + temp[row - 1, col]);
                byte d4 = (byte)(Mask[0, 2] + temp[row - 1, col + 1]);

                temp[row, col] = Math.Min(current, Math.Min(Math.Min(d1, d2), Math.Min(d3, d4)));
            }
        }
        return temp;
    }

    private static byte[,] R_L_Mask(byte[,] input, byte[,] Mask)
    {
        byte[,] temp = new byte[input.GetLength(0), input.GetLength(1)];
        Array.Copy(input, temp, input.Length);

        for (int row = input.GetLength(0) - 2; row >= 1; row--)
        {
            for (int col = input.GetLength(1) - 2; col >= 1; col--)
            {
                byte current = temp[row, col];
                byte d1 = (byte)(Mask[1, 2] + temp[row, col + 1]);
                byte d2 = (byte)(Mask[2, 2] + temp[row + 1, col + 1]);
                byte d3 = (byte)(Mask[2, 1] + temp[row + 1, col]);
                byte d4 = (byte)(Mask[2, 0] + temp[row + 1, col - 1]);

                temp[row, col] = Math.Min(current, Math.Min(Math.Min(d1, d2), Math.Min(d3, d4)));
            }
        }
        return temp;
    }


    public static byte[,] ChamferMatch(byte[,] search, byte[,] reference)
    {
        // Validate input images are binary
        if (!BaseFunctions.isBinaryImage(reference))
        {
            throw new ArgumentException(" reference Inputs of chamfer matching arent Binary!");
        }

        if (!BaseFunctions.isBinaryImage(search))
        {
            throw new ArgumentException("search Inputs of chamfer matching arent Binary!");
        }


        byte[,] dtrans = DistanceTransform(search);

        // Count foreground pixels (255 values) in reference
        int forground = countForegroundPixel(reference);
        if (forground == 0)
        {
            Console.WriteLine("WARNING: No foreground pixels found in template!");
            return new byte[1, 1];
        }

        int hq = search.GetLength(0) - reference.GetLength(0) + 1;
        int wq = search.GetLength(1) - reference.GetLength(1) + 1;
        byte[,] Q = new byte[hq, wq];


        for (int row = 0; row < hq; row++)
        {
            for (int col = 0; col < wq; col++)
            {
                int q = 0;
                for (int y = 0; y < reference.GetLength(0); y++)
                {
                    for (int x = 0; x < reference.GetLength(1); x++)
                    {
                        if (reference[y, x] == 255)  
                        {
                            q += dtrans[row + y, col + x];
                        }
                    }
                }
                Q[row, col] = (byte)(q/forground);
            }
        }
        return Q;
    }


}