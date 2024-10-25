using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;


namespace INFOIBV
{
    internal class TemplateComparisons
    {
        private byte[,] CreateMR (byte field1, byte field2, byte field3, byte field4)
        {

            return new byte[,]
                            {
                                {0,         0,      0  },
                                {0,         0,   field1},
                                {field4, field3, field2}

                            };
        }
        private byte[,] CreateML(byte field1, byte field2, byte field3, byte field4)
        {

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
            byte[,] Mr = new byte[3, 3];
            byte[,] Ml = new byte[3, 3];

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

        public byte[,] ChamferMatch(byte[,] search, byte[,] reference)
        {

        }




    }


  
}
