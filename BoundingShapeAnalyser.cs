using System.Collections.Generic;
using System.Drawing;
using System;


public class OrientedBoundingBox
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Angle { get; set; }  // in degrees
    public (int Y, int X) Center { get; set; }

    public double Area
    {
        get { return Width * Height; }
    }

    public double AspectRatio
    {
        get { return Math.Max(Width, Height) / Math.Min(Width, Height); }
    }
}



public class BoundingShapeAnalyser
{
   

    public static void DrawMinAreaRect(Color[,] image, OrientedBoundingBox rect, Color color)
    {
        // calculate four corners of the rectangle
        double halfWidth = rect.Width / 2;
        double halfHeight = rect.Height / 2;

        (double Y, double X)[] corners = new (double Y, double X)[]
        {
            (-halfHeight, -halfWidth),  // top leftt
            (-halfHeight, halfWidth),   // top right
            (halfHeight, halfWidth),    // bottom right
            (halfHeight, -halfWidth)    // bottom left
        };

        // totate corners and translate to center position
        (int Y, int X)[] rotatedCorners = new (int Y, int X)[4];
        for (int i = 0; i < 4; i++)
        {
            (int Y, int X) rotated = RotatePoint(corners[i].Y, corners[i].X, -rect.Angle);
            rotatedCorners[i] = (
                Y: rotated.Y + rect.Center.Y,
                X: rotated.X + rect.Center.X
            );
        }

        // draw lines between corners
        for (int i = 0; i < 4; i++)
        {
            int nextIndex = (i + 1) % 4;
            DrawLine(
                image,
                rotatedCorners[i].X, rotatedCorners[i].Y,
                rotatedCorners[nextIndex].X, rotatedCorners[nextIndex].Y,
                color
            );
        }
    }

    //using bresenhams line algorithm
    private static void DrawLine(Color[,] image, int x0, int y0, int x1, int y1, Color color)
    {
        int imageHeight = image.GetLength(0);
        int imageWidth = image.GetLength(1);

       
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // Only draw if point is within image bounds
            if (y0 >= 0 && y0 < imageHeight && x0 >= 0 && x0 < imageWidth)
            {
                image[y0, x0] = color;
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public static OrientedBoundingBox GetMinOBBox(List<(int Y, int X)> contour)
    {
        OrientedBoundingBox minRect = new OrientedBoundingBox();
        double minArea = double.MaxValue;

        for (double angle = 0; angle < 90; angle += 0.5)
        {
            List<(int Y, int X)> rotatedPoints = new List<(int Y, int X)>();
            foreach ((int Y, int X) point in contour)
            {
                rotatedPoints.Add(RotatePoint(point.Y, point.X,angle));
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach ((int Y, int X) point in rotatedPoints)
            {
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
            }

            double width = maxX - minX;
            double height = maxY - minY;
            double area = width * height;

            if (area < minArea)
            {
                minArea = area;
                minRect.Width = width;
                minRect.Height = height;
                minRect.Angle = angle;

                int centerY = (minY + maxY) / 2;
                int centerX = (minX + maxX) / 2;
                minRect.Center = RotatePoint(centerY, centerX, -angle);
            }
        }

        return minRect;
    }

    public static double getAreaBoundingRatio(OrientedBoundingBox minRect, double regionArea)
    {
        return  regionArea / minRect.Area;
    }

    private static (int Y, int X) RotatePoint(double y, double x, double angleDegrees)
    {
        double angleRad = angleDegrees * Math.PI / 180.0;
        double cos = Math.Cos(angleRad);
        double sin = Math.Sin(angleRad);

        int newX = (int)(x * cos + -y * sin);  // X is calculated first
        int newY = (int)((x * sin) + y * cos);  // Y follows

        return (newY, newX);  // Return in (Y, X) format
    }
}