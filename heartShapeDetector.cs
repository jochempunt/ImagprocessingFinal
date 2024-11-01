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
        private const int MIN_CIRCLES_FOR_HEART = 2;
        private const int MAX_CIRCLES_FOR_HEART = 3;

        private const float MAX_CIRCLE_OVERLAP_RATIO = 0.2f;  // how much circles can overlap
        private const float MAX_CIRCLE_SEPARATION_RATIO = 0.3f;

        private const float MAX_VERTICAL_OFFSET_RATIO = 0.1f;  // maximum allowed vertical misalignment between circles
        public static bool isHeartBasedOnCircles(AxisAlignedBoundingBox aabb, List<CircleDetectorHough.Circle> circles)
        {
            if (circles.Count < MIN_CIRCLES_FOR_HEART || circles.Count > MAX_CIRCLES_FOR_HEART) return false;

            int Xdistance = Math.Abs((circles[0].CenterX - circles[1].CenterX)) - (circles[0].Radius + circles[1].Radius);
            int minOverlapDistance = -(int)(MAX_CIRCLE_OVERLAP_RATIO * (circles[0].Radius + circles[1].Radius)); // Small overlap allowed
            int maxProximityDistance = (int)(MAX_CIRCLE_SEPARATION_RATIO * (circles[0].Radius + circles[1].Radius)); // Maximum allowed distance
            int Ydistance = Math.Abs((circles[0].CenterY - circles[1].CenterY));

            int maxYdistance = (int)Math.Round(aabb.Height * MAX_VERTICAL_OFFSET_RATIO);
            int topAlignment = (int)Math.Round(aabb.Height / 2);

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
