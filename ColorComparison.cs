using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Drawing;

namespace INFOIBV
{
    internal class ColorComparison
    {
        const double MIN_RED_HUE = 340;
        const double MAX_RED_HUE = 40;
        const double MIN_SATURATION = 0.2;
        const double RED_THRESHOLD = 0.8;
        public static bool isRedColor(Region regionG, Color[,] colourImage)
        {
            bool isRed = false;
            double redPixels = 0;
            
            foreach ((int y, int x) in regionG.Pixels)
            {    
                byte red = colourImage[y, x].R;      
                byte green = colourImage[y, x].G; 
                byte blue = colourImage[y, x].B;   

                var (hue, saturation, value) = RGBtoHSV(red, green, blue);

                if (saturation > MIN_SATURATION)
                {

                    if ((hue >= 0 && hue <= MAX_RED_HUE) || (hue >= MIN_RED_HUE && hue <= 360))
                    {
                        redPixels += 1;
                    }
                }


            }

            double colorAvg = redPixels / regionG.Pixels.Count;
            if (colorAvg > RED_THRESHOLD)
            {
                isRed = true;
            }

            return isRed;
        }


        private static (double Hue, double Saturation, double Value) RGBtoHSV(int r, int g, int b)
        {
            double red = r / 255.0;
            double green = g / 255.0;
            double blue = b / 255.0;

            double max = Math.Max(red, Math.Max(green, blue));
            double min = Math.Min(red, Math.Min(green, blue));
            double delta = max - min;

            double hue = 0.0;
            if (delta > 0)
            {
                if (max == red)
                    hue = 60 * (((green - blue) / delta + 6) % 6);
                else if (max == green)
                    hue = 60 * ((blue - red) / delta + 2);
                else
                    hue = 60 * ((red - green) / delta + 4);
            }

            double saturation = max == 0 ? 0 : (delta / max);
            double value = max;

            return (hue, saturation, value);
        }

    }
}