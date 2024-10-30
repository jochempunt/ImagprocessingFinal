using System.Collections.Generic;
using System.Drawing;
using System;

namespace INFOIBV
{


    public class AxisAlignedBoundingBox
    {
        public double Width { get; set; }
        public double Height { get; set; }
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

    public class OrientedBoundingBox : AxisAlignedBoundingBox
    {
        public double Angle { get; set; }  // in degrees
    }



    public class BoundingShapeAnalyser
    {

        /// <summary>
        /// Draws bounding box, allows to draw oriented but also axis aligned bounding boxes
        /// </summary>
        /// <param name="image"></param>
        /// <param name="box"></param>
        /// <param name="color"></param>
        public static void DrawBoundingBox(Color[,] image, AxisAlignedBoundingBox box, Color color)
        {
            // Calculate four corners of the rectangle
            double halfWidth = box.Width / 2;
            double halfHeight = box.Height / 2;
            (double Y, double X)[] corners = new (double Y, double X)[]
            {
            (-halfHeight, -halfWidth),  // top left
            (-halfHeight, halfWidth),   // top right
            (halfHeight, halfWidth),    // bottom right
            (halfHeight, -halfWidth)    // bottom left
            };

            // Get the angle - 0 for AABB, specified angle for OBB
            double angle = (box is OrientedBoundingBox obb) ? -obb.Angle : 0;

            // Rotate corners and translate to center position
            (int Y, int X)[] rotatedCorners = new (int Y, int X)[4];
            for (int i = 0; i < 4; i++)
            {
                (int Y, int X) rotated = RotatePoint(corners[i].Y, corners[i].X, angle);
                rotatedCorners[i] = (
                    Y: rotated.Y + box.Center.Y,
                    X: rotated.X + box.Center.X
                );
            }

            // Draw lines between corners
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

        public static AxisAlignedBoundingBox GetAABB(List<(int Y, int X)> outerContour)
        {
            if (outerContour == null || outerContour.Count == 0)
                throw new ArgumentException("Contour cannot be null or empty");

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var point in outerContour)
            {
                minX = Math.Min(minX, point.X);
                maxX = Math.Max(maxX, point.X);
                minY = Math.Min(minY, point.Y);
                maxY = Math.Max(maxY, point.Y);
            }

            return new AxisAlignedBoundingBox
            {
                Width = maxX - minX,
                Height = maxY - minY,
                Center = ((minY + maxY) / 2, (minX + maxX) / 2)
            };
        }

        public static OrientedBoundingBox GetMinOBBox(List<(int Y, int X)> contour)
        {
            if (contour == null || contour.Count == 0)
                throw new ArgumentException("Contour cannot be null or empty");

            OrientedBoundingBox minRect = new OrientedBoundingBox();
            double minArea = double.MaxValue;

            for (double angle = 0; angle < 90; angle += 0.5)
            {
                List<(int Y, int X)> rotatedPoints = new List<(int Y, int X)>();
                for (int i = 0; i < contour.Count; i++)
                {
                    rotatedPoints.Add(RotatePoint(contour[i].Y, contour[i].X, angle));
                }

                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minY = int.MaxValue;
                int maxY = int.MinValue;

                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    var point = rotatedPoints[i];
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
                    minRect.Center = RotatePoint((minY + maxY) / 2, (minX + maxX) / 2, -angle);
                }
            }

            return minRect;
        }

        public static double getAreaBoundingRatio(OrientedBoundingBox minRect, double regionArea)
        {
            return regionArea / minRect.Area;
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


        public static byte[,] RotateRegionToUpright(byte[,] sourceImage, OrientedBoundingBox boundingBox)
        {
            var (centerY, centerX) = boundingBox.Center;
            double angleDegrees = -boundingBox.Angle;

            // Determine if we need an additional 90° rotation to make longer side vertical
            bool needsAdditional90 = boundingBox.Width > boundingBox.Height;
            if (needsAdditional90)
            {
                angleDegrees += 90;
            }

            int originalWidth = sourceImage.GetLength(1);
            int originalHeight = sourceImage.GetLength(0);

            // If we did a 90° rotation, swap width and height
            int newHeight = (int)(needsAdditional90 ? boundingBox.Width : boundingBox.Height);
            int newWidth = (int)(needsAdditional90 ? boundingBox.Height : boundingBox.Width);
            byte[,] rotatedRegionImage = new byte[newHeight, newWidth];

            for (int newY = 0; newY < newHeight; newY++)
            {
                for (int newX = 0; newX < newWidth; newX++)
                {
                    double relativeY = newY - (newHeight / 2.0);
                    double relativeX = newX - (newWidth / 2.0);

                    var (rotatedY, rotatedX) = RotatePoint(relativeY, relativeX, angleDegrees);

                    double sourceY = rotatedY + centerY;
                    double sourceX = rotatedX + centerX;

                    if (sourceX >= 0 && sourceX < originalWidth - 1 &&
                        sourceY >= 0 && sourceY < originalHeight - 1)
                    {
                        int x1 = (int)Math.Floor(sourceX);
                        int y1 = (int)Math.Floor(sourceY);
                        int x2 = x1 + 1;
                        int y2 = y1 + 1;

                        double wx = sourceX - x1;
                        double wy = sourceY - y1;

                        byte p11 = sourceImage[y1, x1];
                        byte p12 = sourceImage[y1, x2];
                        byte p21 = sourceImage[y2, x1];
                        byte p22 = sourceImage[y2, x2];

                        rotatedRegionImage[newY, newX] = BilinearInterpolation(p11, p12, p21, p22, wx, wy);
                    }
                }
            }

            return rotatedRegionImage;
        }


        public static byte[,] ExtractAABBContent(byte[,] sourceImage, AxisAlignedBoundingBox boundingBox, double paddingRatio = 0.06f)
        {
            int originalWidth = sourceImage.GetLength(1);
            int originalHeight = sourceImage.GetLength(0);

            // Calculate padding amounts
            float widthPadding =(float) (boundingBox.Width * paddingRatio);
            float heightPadding = (float)(boundingBox.Height * paddingRatio);

            // Get the dimensions of the padded bounding box
            int newWidth = (int)(boundingBox.Width + 2 * widthPadding);
            int newHeight = (int)(boundingBox.Height + 2 * heightPadding);

            // Calculate the top-left corner from the center, including padding
            int startY = (int)(boundingBox.Center.Y - (boundingBox.Height / 2 + heightPadding));
            int startX = (int)(boundingBox.Center.X - (boundingBox.Width / 2 + widthPadding));

            // Create the output image with the padded size
            byte[,] extractedContent = new byte[newHeight, newWidth];

            // Copy the pixels from the source image to the new image
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    int sourceY = startY + y;
                    int sourceX = startX + x;

                    // Check if we're within the bounds of the source image
                    if (sourceX >= 0 && sourceX < originalWidth &&
                        sourceY >= 0 && sourceY < originalHeight)
                    {
                        extractedContent[y, x] = sourceImage[sourceY, sourceX];
                    }
                }
            }

            return extractedContent;
        }


        private static byte BilinearInterpolation(byte p11, byte p12, byte p21, byte p22, double wx, double wy)
        {
            return (byte)((1 - wx) * ((1 - wy) * p11 + wy * p21) + wx * ((1 - wy) * p12 + wy * p22));
        }

    }
}