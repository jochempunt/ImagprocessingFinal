using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;


namespace INFOIBV
{
    internal class TemplateComparisons
    {
        // We use the eucledian version of the MR of ML
        private byte[,] CreateMR (int scale)
        {
            byte field1 = (byte) (1 * scale);
            byte field2 = (byte)(Math.Sqrt(2) * scale);
            byte field3 = (byte)(1 * scale);
            byte field4 = (byte)(Math.Sqrt(2) * scale);

            return new byte[,]
                            {
                                {0,         0,      0  },
                                {0,         0,   field1},
                                {field4, field3, field2}

                            };
        }
        private byte[,] CreateML(int scale)
        {
            byte field1 = (byte)(1 * scale);
            byte field2 = (byte)(Math.Sqrt(2) * scale);
            byte field3 = (byte)(1 * scale);
            byte field4 = (byte)(Math.Sqrt(2) * scale);


            return new byte[,]
                            {
                                {field2, field3, field4  },
                                {field1,   0,      0     },
                                {  0,      0,      0     }

                            };
        }
        
        private float EucledianDistance((float x, float y) p1, (float a, float b) p2)
        {

            return (float)Math.Sqrt((p2.a - p1.x) * (p2.a - p1.x) + (p2.b - p1.y) * (p2.b - p1.y));
        }

        private byte[,] L_R_Mask(byte[,] input, byte[,] Mask)
        {
            byte[,] temp = new byte[input.GetLength(0), input.GetLength(1)];
            for (int row = 0; row < input.GetLength(0) - 1; row++)
                for (int col = 0; col < input.GetLength(1) - 2; col++)
                {
                    int mHeight = Mask.GetLength(0);
                    int mWidth = Mask.GetLength(1);
                    byte d1 = (byte)(Mask[mHeight / 2, 0] + input[row, col - 1]);
                    byte d2 = (byte)(Mask[0, 0] + input[row - 1, col - 1]);
                    byte d3 = (byte)(Mask[0, mWidth / 2] + input[row - 1, col]);
                    byte d4 = (byte)(Mask[0, mWidth] + input[row - 1, col + 1]);
                    temp[row, col] = new[] { d1, d2, d3, d4 }.Min();
                }

            return temp;

        
        }
        private byte[,] R_L_Mask(byte[,] input, byte[,] Mask)
        {
            byte[,] temp = new byte[input.GetLength(0), input.GetLength(1)];
            for (int row = 0; row < input.GetLength(0) -2; row++)
                for (int col = 0; col < input.GetLength(1) - 2; col++)
                {
                    int mHeight = Mask.GetLength(0);
                    int mWidth = Mask.GetLength(1);
                    byte d1 = (byte)(Mask[mHeight / 2, 0] + input[row, col + 1]);
                    byte d2 = (byte)(Mask[0, 0] + input[row + 1, col + 1]);
                    byte d3 = (byte)(Mask[0, mWidth / 2] + input[row + 1, col]);
                    byte d4 = (byte)(Mask[0, mWidth] + input[row + 1, col - 1]);
                    temp[row, col] = new[] { d1, d2, d3, d4 }.Min();
                }

            return temp;


        }
        private byte[,] DistanceTransform(byte[,] input)
        {
            int Heigth = input.GetLength(0);
            int Width = input.GetLength(1); 


            byte[,] result = new byte[Heigth, Width];
            byte[,] Mr = CreateMR(1);
            byte[,] Ml = CreateML(1);

            for (int row = 0; row < Heigth; row++)
                for (int col = 0; col < Width; col++)
                {
                    int i = input[row, col];
                    if (i == 1)
                        result[row, col] = 0;
                    else
                        result[row, col] = 255;
                }
            L_R_Mask(result, Ml);
            R_L_Mask(result, Ml);

            return result;
                    
                    
        }

        private int countForegroundPixel(byte[,] input)
        {
            int foregroundCount = 0; // Initialize a counter for foreground pixels

            // Loop through each element in the 2D byte array
            for (int row = 0; row < input.GetLength(0); row++)
            {
                for (int col = 0; col < input.GetLength(1); col++)
                {
                    // Check if the current pixel is a foreground pixel
                    if (input[row, col] == 1)
                    {
                        foregroundCount++; // Increment the counter
                    }
                }
            }

            return foregroundCount; // Return the total count of foreground pixels
        }
        public byte[,] ChamferMatch(byte[,] search, byte[,] reference)
        {
            byte[,] dtrans = DistanceTransform(search);
            int forgound =  countForegroundPixel(reference);
            int hq = search.GetLength(0) - reference.GetLength(0) + 1;
            int wq = search.GetLength(1) - reference.GetLength(1) + 1;
            byte[,] Q = new byte[hq, wq];

            for (int row = 0; row < hq - 1; row++)
                for(int col = 0; col < wq - 1; col++)
                {
                    int q = 0;
                    for (int y = 0; y < reference.GetLength(0) - 1; y++)
                        for (int x = 0; x < reference.GetLength(1) - 1; x++)
                        {
                            if (reference[y, x] == 1)
                                q = q + dtrans[row + y, col + x];
                        }
                    Q[row, col] = (byte)(q / forgound);

                }
            return Q;
        }




    }


  
}
