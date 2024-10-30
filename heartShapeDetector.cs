using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INFOIBV
{
    internal class heartShapeDetector
    {
        public static bool isHeartBasedOnCircles(AxisAlignedBoundingBox aabb, List<CircleDetectorHough.Circle> circles)
        {
            if (circles.Count < 2 || circles.Count > 3) return false;
            int Xdistance = Math.Abs((circles[0].CenterX - circles[1].CenterX)) - (circles[0].Radius + circles[1].Radius);
            int minOverlapDistance = -(int)(0.2 * (circles[0].Radius + circles[1].Radius)); // Small overlap allowed
            int maxProximityDistance = (int)(0.3 * (circles[0].Radius + circles[1].Radius)); // Maximum allowed distance
            int Ydistance = Math.Abs((circles[0].CenterY - circles[1].CenterY));

            int maxYdistance = (int)Math.Round(aabb.Height * 0.1);
            Console.WriteLine($"proximitiy of circle 1 and 2: height= {Ydistance} & width= {Xdistance} ");
            Console.WriteLine($"max overlap distance= {minOverlapDistance} & max proximity distance= {maxProximityDistance} ");
            int topAlignment = (int)Math.Round(aabb.Height / 2);
            Console.WriteLine($"top alignment= {topAlignment} & max Y distance= {maxYdistance} ");

            if (circles.Count == 3 && circles[2].CenterY <= topAlignment) return false;
        

            if (circles[0].CenterY < topAlignment && circles[1].CenterY < topAlignment)
            {
                if (Ydistance <= maxYdistance && Xdistance >= minOverlapDistance && Xdistance <= maxProximityDistance)
                {

                    Console.WriteLine("region is probably a heart!");
                    return true;
                }

            }

            return false;

        }


    }
}
