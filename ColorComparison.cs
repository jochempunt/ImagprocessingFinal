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
        public bool CompareColor(Region regionG, Color[,] colourImage)
        {
            // Define the RGB thresholds and hue range for identifying "red"
            const byte redThreshold = 150;         // Minimum red channel value
            const byte greenMaxThreshold = 100;    // Maximum green channel value for "red"
            const byte blueMaxThreshold = 100;     // Maximum blue channel value for "red"
            const double minRedHue = 340;          // Minimum hue for red (in degrees)
            const double maxRedHue = 20;           // Maximum hue for red (in degrees)

            bool hasRedPixels = false;

                foreach ((int y, int x) in regionG.Pixels)
                {
                    // Access the color image at (x, y)
                    byte red = colourImage[y, x ].R;        // Red channel
                    byte green = colourImage[y, x ].G;  // Green channel
                    byte blue = colourImage[y, x ].B;   // Blue channel

                    // Check if the pixel meets basic RGB "red" thresholds
                    if (red >= redThreshold && green <= greenMaxThreshold && blue <= blueMaxThreshold)
                    {
                        // Use RGBtoHSV to get hue, saturation, and value
                        var (hue, saturation, value) = RGBtoHSV(red, green, blue);

                        // Check if hue is within red range (either between 0-20 or 340-360)
                        if ((hue >= 0 && hue <= maxRedHue) || (hue >= minRedHue && hue <= 360))
                        {
                            hasRedPixels = true;
                            break;  // No need to continue if we find a red pixel
                        }
                    }
                }
            return hasRedPixels;
        }
          
        
        private (double Hue, double Saturation, double Value) RGBtoHSV(int r, int g, int b)
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