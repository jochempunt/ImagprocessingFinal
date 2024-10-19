using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;


namespace INFOIBV
{
    public enum PaddingFunctions
    {
        ZeroPadding,
        BorderExtension,
        ReflectPadding
    }
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;

        /*
         * this enum defines the processing functions that will be shown in the dropdown (a.k.a. combobox)
         * you can expand it by adding new entries to applyProcessingFunction()
         */
        private enum ProcessingFunctions
        {
            Invert,
            AdjustContrast,
            ConvolutionFilter,
            MedianFilter,
            DetectEdges,
            Threshold,
            Pipeline1,
            Pipeline2,
            Canny,
            HistogramEq,
            Dilation_grayscale,
            Erosion_grayscale,
            Dilation_binary,
            Erosion_binary,
            Open_grayscale,
            Open_binary,
            Close_grayscale,
            Close_binary,
            AND,
            OR,
            BOUNDARYS,
            addNoise,
            removeNoise,
            houghTransform,
            detectLineSegments,
            HTAngleLimits,
            HTNormalised,
            DetectCircles
        }




        /*
         * these are the parameters for your processing functions, you should add more as you see fit
         * it is useful to set them based on controls such as sliders, which you can add to the form
         */
        private byte filterSize = 3;
        private float filterSigma = 2f;
        private byte threshold = 127;




        private string filterform = "Square";




        public INFOIBV()
        {
            InitializeComponent();
            populateCombobox();
            //imageProcessor = new ImageProcessor();
            initValues();
        }
        private void PrintTuples(List<(int, int)> tuples)
        {
            if (tuples == null || tuples.Count == 0)
            {
                Console.WriteLine("No tuples to display.");
                return;
            }

            Console.WriteLine("Boundary Points:");
            foreach (var tuple in tuples)
            {
                Console.WriteLine($"({tuple.Item1}, {tuple.Item2})");
            }
        }



        /*
         * populateCombobox: populates the combobox with items as defined by the ProcessingFunctions enum
         */
        private void populateCombobox()
        {
            foreach (string itemName in Enum.GetNames(typeof(ProcessingFunctions)))
            {
                string ItemNameSpaces = Regex.Replace(Regex.Replace(itemName, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
                comboBox.Items.Add(ItemNameSpaces);
            }
            comboBox.SelectedIndex = 0;
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }




        /*
         * loadButton_Click: process when user clicks "Load" button
         */
        private void loadImageButton_Click(object sender, EventArgs e)
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // open file dialog
            {
                string file = openImageDialog.FileName;                     // get the file name
                imageFileName.Text = file;                                  // show file name
                if (InputImage != null) InputImage.Dispose();               // reset image
                InputImage = new Bitmap(file);                              // create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // dimension check (may be removed or altered)
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                {
                    byte[,] grayscaleImage = ImageConverter.BitmapToGrayscale(InputImage);
                    pictureBox1.Image = (Image)InputImage;                 // display input image
                }
            }
        }



        /*
         * applyButton_Click: process when user clicks "Apply" button
         */
        private void applyButton_Click(object sender, EventArgs e)
        {

            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            try
            {
                byte[,] workingImage = ImageConverter.BitmapToGrayscale(InputImage);
                byte[,] processedImage = ProcessImage(workingImage);
                OutputImage = ImageConverter.GrayscaleToBitmap(processedImage);
                pictureBox2.Image = OutputImage;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
        }

        /*
         * applyProcessingFunction: defines behavior of function calls when "Apply" is pressed
         */
        private byte[,] ProcessImage(byte[,] workingImage)
        {

            int height = workingImage.GetLength(0);
            int width = workingImage.GetLength(1);

            switch ((ProcessingFunctions)comboBox.SelectedIndex)
            {
                case ProcessingFunctions.Invert:
                    return Preprocessor.InvertImage(workingImage);
                case ProcessingFunctions.AdjustContrast:
                    return Preprocessor.adjustContrast(workingImage);
                case ProcessingFunctions.ConvolutionFilter:
                    return Preprocessor.applyGaussianFilter(workingImage, 1f);
                case ProcessingFunctions.MedianFilter:
                    return Preprocessor.applyMedianFilter(workingImage, filterSize);
                case ProcessingFunctions.DetectEdges:
                    return EdgeDetector.getEdgeMagnitude(workingImage);
                case ProcessingFunctions.Threshold:
                    return Preprocessor.thresholdImage(workingImage, threshold);
                case ProcessingFunctions.Canny:
                    return EdgeDetector.detectEdgesCanny(workingImage, 200, 250, 1.5f);
                case ProcessingFunctions.HistogramEq:
                    return Preprocessor.HistogramEqual(workingImage);
                case ProcessingFunctions.Dilation_grayscale:
                    return Morphology.Dilate(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Grayscale);
                case ProcessingFunctions.Erosion_grayscale:
                    return Morphology.Erode(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Grayscale);
                case ProcessingFunctions.Dilation_binary:
                    return Morphology.Dilate(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary);
                case ProcessingFunctions.Erosion_binary:
                    return Morphology.Erode(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary);
                case ProcessingFunctions.Open_binary:
                    return Morphology.Open(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary);
                case ProcessingFunctions.Open_grayscale:
                    return Morphology.Open(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Grayscale);
                case ProcessingFunctions.Close_binary:
                    return Morphology.Close(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary);
                case ProcessingFunctions.Close_grayscale:
                    return Morphology.Close(workingImage, Morphology.ElementShape.Square, 3, Morphology.ImageType.Grayscale);
                default:
                    return null;
            }
        }

        /*
         * saveButton_Click: process when user clicks "Save" button
         */
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // save the output image
        }

        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 2 GO HERE ==============
        // ====================================================================
       


        private void INFOIBV_Load(object sender, EventArgs e)
        {

        }

        

        /*
         * traceBoundary: traces the outer boundary of a foreground object in a binary image.
         * input:   binaryImage     byte[,] array representing the binary image (0 for background, 255 for foreground)
         * output:                  returns a list of (int, int) tuples representing the boundary coordinates (y, x)
         */
        private List<(int, int)> traceBoundary(byte[,] binaryImage)
        {
            if (!BaseFunctions.isBinaryImage(binaryImage))
            {
                throw new ArgumentException("image for tracing boundary is not a binary image");
            }
            int width = binaryImage.GetLength(1);
            int height = binaryImage.GetLength(0);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (binaryImage[y, x] == 255)
                    {
                        (int, int) startP = (y, x);
                        return traceContour(binaryImage, startP, 7);
                    }
                }
            }
            //if no boundary is found, return an empty list
            return new List<(int, int)>();
        }

        /*
         * traceContour: traces a closed contour around a foreground object in a binary image starting from a given point.
         * input:   binaryImage     byte[,] array representing the binary image (0 for background, 255 for foreground)
         *          startingPoint   tuple (int, int) representing the coordinates (y, x) to start tracing the contour
         *          initalDirection int representing the initial direction to search for the contour (use 7 for outer contours)
         * output:                  returns a list of (int, int) tuples representing the contour coordinates (y, x)
         */
        private List<(int, int)> traceContour(byte[,] binaryImage, (int, int) startingPoint, int initalDirection)
        {
            bool done = false;
            List<(int, int)> contour = new List<(int, int)>();

            (int firstY, int firstX, int nextDirection) = findNextPoint(binaryImage, startingPoint, initalDirection);
            contour.Add((firstY, firstX));
            //handle isolated pixel
            if (startingPoint.Equals((firstY, firstX)))
            {
                return contour;
            }

            (int prevY, int prevX) = (startingPoint.Item1, startingPoint.Item2);
            (int currY, int currX) = (firstY, firstX);
            (int nextY, int nextX) = (-1, -1);
            int direction = initalDirection;
            while (!done)
            {
                direction = (nextDirection + 6) % 8;
                (nextY, nextX, nextDirection) = findNextPoint(binaryImage, (currY, currX), direction);

                (prevY, prevX) = (currY, currX);
                (currY, currX) = (nextY, nextX);
                Console.WriteLine($" Previous: ({prevY}, {prevX}), Start: ({startingPoint.Item1},{startingPoint.Item2}) Current: ({currY}, {currX}), Start: ({firstY},{firstX})");
                if ((prevY, prevX).Equals(startingPoint) && (currY, currX).Equals((firstY, firstX)))
                {
                    done = true;
                }
                else
                {
                    contour.Add((nextY, nextX));
                }

            }
            return contour;
        }

        //x and y are swapped for convenience (since an 2d array/ image here is indexed like a[y,x]
        private (int, int)[] Dirs = { (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1) };

        /*
         * findNextPoint: finds the next foreground pixel (255) in a binary image, starting from a given point and searching in a specified direction.
         * input:   startingPoint    tuple (int, int) representing the current pixel coordinates (y, x)
         *          binaryImage      byte[,] array representing the binary image (0 for background, 255 for foreground)
         *          direction        initial search direction (0-7 corresponding to surrounding pixels)
         * output:                   returns a tuple (int, int, int) with the coordinates (y, x) of the next foreground pixel and the new search direction
         */
        private (int, int, int) findNextPoint(byte[,] binaryImage, (int, int) startingPoint, int direction)
        {
            int newDirection = direction;
            for (int i = 0; i < 8; i++)
            {
                int nextX = startingPoint.Item2 + Dirs[newDirection].Item2;
                int nextY = startingPoint.Item1 + Dirs[newDirection].Item1;
                Console.WriteLine(nextY + " " + nextX);
                byte newVal = 0;
                // Ensure we stay within bounds
                if (nextX >= 0 && nextX < binaryImage.GetLength(1) &&
                    nextY >= 0 && nextY < binaryImage.GetLength(0))
                {
                    newVal = binaryImage[nextY, nextX];
                    if (newVal == 255)
                    {
                        return (nextY, nextX, newDirection);
                    }
                }
                newDirection = (newDirection + 1) % 8;

            }
            return (startingPoint.Item1, startingPoint.Item2, direction);
        }

        /*
         * drawBoundaries: draws the boundary of a shape in a binary image by setting boundary points to 255.
         * input:   image      byte[,] array representing the binary image
         * output:             returns a new byte[,] array where boundary pixels are set to 255, others remain 0
         */
        private byte[,] drawBoundaries(byte[,] image)
        {
            int width = image.GetLength(1);
            int height = image.GetLength(0);
            byte[,] result = new byte[height, width];

            List<(int, int)> boundaries = traceBoundary(image);

            foreach ((int, int) point in boundaries)
            {
                result[point.Item1, point.Item2] = 255;
            }
            return result;
        }


        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 3 GO HERE ==============
        // ====================================================================

        /*
         * houghTransform: compute the Hough transform for line detection
         * input:   inputImage          binary or grayscale image
         *          thetaStepDegree     angular step for Hough transform
         *          rhoStep             radial step for Hough transform
         * output:  Hough accumulator   array of accumulator values
         */
        private int[,] houghTransform(byte[,] inputImage, float thetaStepDegree, float rhoStep)
        {
            int imgHeight = inputImage.GetLength(0);
            int imgWidth = inputImage.GetLength(1);

            if (thetaStepDegree <= 0 || rhoStep <= 0)
                throw new ArgumentException("Step sizes must be positive.");

            // the max size rho can have, the largest distance of a square image, is the diagonal
            double diagonal = (Math.Sqrt(imgWidth * imgWidth + imgHeight * imgHeight));
            int thetaCount = (int)(180 / thetaStepDegree);
            int rhoCount = (int)(diagonal / rhoStep);
            bool binaryImage = BaseFunctions.isBinaryImage(inputImage);
            Console.WriteLine($"accumulator [{rhoCount},{thetaCount}], diagonal = {diagonal}, rhocount = {rhoCount}");

            int[,] accumulator = new int[thetaCount, rhoCount];

            // we can precompute cosTheta and sinTheta for every theta step
            double[] cosTheta = new double[thetaCount];
            double[] sinTheta = new double[thetaCount];

            for (int t = 0; t < thetaCount; t++)
            {
                double theta = (t * thetaStepDegree) * Math.PI / 180.0; // get the theta from 0-180 and convert to radian
                cosTheta[t] = Math.Cos(theta);
                sinTheta[t] = Math.Sin(theta);
            }

            // for shifting the image center to the middle 
            double centerY = imgHeight / 2.0;
            double centerX = imgWidth / 2.0;

            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    if (inputImage[y, x] > 0)
                    {
                        double cX = x - centerX;
                        double cY = y - centerY;

                        for (int t = 0; t < thetaCount; t++)
                        {
                            double rho = cX * cosTheta[t] + cY * sinTheta[t]; // we dont need to re caculate t or cos / sin since we pre calculated them

                            // shift negative values , and scale them so they fit in the accumulator range
                            int rIndex = (int)Math.Round((rho + diagonal / 2) / rhoStep);

                            if (rIndex >= 0 && rIndex < rhoCount)
                            {
                                if (binaryImage)
                                {
                                    accumulator[t, rIndex]++;
                                }
                                else
                                {
                                    //if greyscale image, we add the value
                                    accumulator[t, rIndex] += inputImage[y, x];
                                }
                            }
                        }
                    }

                }
            }
            return accumulator;
        }


        /*
         * visualiseAccumulator: create a visual representation of the Hough accumulator
         * input:   accumulator         hough accumulator
         *          makeSquare          whether to resize output to a square
         * output:  accumulatorImg      byte array of normalized accumulator values for visualization
         */
        private byte[,] visualiseAccumulator(int[,] accumulator, bool makeSquare = false)
        {

            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);


            int outputHeight = makeSquare ? 512 : height;
            int outputWidth = makeSquare ? 512 : width;

            // create  byte array for the accumulator image
            byte[,] accumulatorImg = new byte[outputHeight, outputWidth];

            // fin  max  value for normalization
            int maxVal = accumulator.Cast<int>().Max();

            for (int y = 0; y < outputHeight; y++)
            {
                for (int x = 0; x < outputWidth; x++)
                {
                    int cx = (int)(x * width / outputWidth);
                    int cy = (int)(y * height / outputHeight);

                    int thetaIndex = Math.Min(cx, width - 1); //x corresponds to theta
                    int rhoIndex = Math.Min(cy, height - 1); // y corresponds to rho



                    int val = accumulator[rhoIndex, thetaIndex];

                    // normalize the value for byte range
                    int normalizedValue = (maxVal > 1) ? (int)(val * 255.0 / maxVal) : 0;
                    accumulatorImg[y, x] = (byte)normalizedValue;
                }
            }
            return accumulatorImg;
        }


        /*
         * peakFinding: find peaks in the Hough transform accumulator
         * input:   inputImage        binary image
         *          thresholdInPercent threshold for peak detection
         *          thetaStepDegree    angular step for Hough transform
         *          rhoStep            radial step for Hough transform
         *          lowerAngleLimit     minimum angle limit
         *          upperAngleLimit     maximum angle limit
         * output:  list of detected peaks (rho, theta, value)
         */
        public List<(double r, double theta, int value)> peakFinding(byte[,] inputImage, float thresholdInPercent, float thetaStepDegree, float rhoStep, float lowerAngleLimit = 0f, float upperAngleLimit = 180f)
        {
            int imgHeight = inputImage.GetLength(0);
            int imgWidth = inputImage.GetLength(1);

            //int[,] accumulator = houghTransform(inputImage, thetaStepDegree, rhoStep);

            int[,] accumulator = houghTransformAngleLimits(inputImage, thetaStepDegree, rhoStep, lowerAngleLimit, upperAngleLimit);

            // determine threshold value
            int thresholdValue = findThreshold(accumulator, thresholdInPercent);

            //apply non maximum supression
            int[,] supressedAccumulator = applyNMSQuadratic(accumulator, 3);

            //find peaks
            double diagonal = (Math.Sqrt(imgWidth * imgWidth + imgHeight * imgHeight));
            List<(double r, double theta, int value)> peaks = findPeaks(supressedAccumulator, thresholdValue, diagonal, thetaStepDegree, rhoStep, lowerAngleLimit);

            Console.WriteLine($" peak amount: {peaks.Count} with a threshold of {thresholdValue}");
            printPeaks(peaks);

            return peaks;
        }


        /*
         * findThreshold: calculate threshold from accumulator
         * input:   accumulator      Hough accumulator
         *          percentage       percentage of the max value
         * output:  threshold value
         */
        private int findThreshold(int[,] accumulator, float percentage)
        {
            int max = accumulator.Cast<int>().Max();
            return (int)(max * percentage);
        }


        /*
         * printPeaks: print the detected peaks to console
         * input:   peaks            list of detected peaks (rho, theta, value)
         * output:                   prints peak values
         */
        private void printPeaks(List<(double r, double theta, int value)> peaks)
        {
            for (int i = 0; i < peaks.Count; i++)
            {
                Console.WriteLine($"rho:{peaks[i].r}, theta:{peaks[i].theta}, value/crossings: {peaks[i].value}");
            }
        }

        /*
         * applyNMSQuadratic: apply non-maximum suppression to accumulator
         * input:   accumulator      Hough accumulator
         *          windowSize       size of the neighborhood window
         * output:  NMS accumulator
         */
        private int[,] applyNMSQuadratic(int[,] accumulator, int windowSize = 3)
        {
            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);
            int[,] nmsAccumulator = new int[height, width];

            for (int theta = 0; theta < height; theta++)
            {
                for (int r = 0; r < width; r++)
                {
                    int centerValue = accumulator[theta, r];
                    bool isLocalMaximum = true;

                    // Check neighborhood
                    for (int dTheta = -windowSize / 2; dTheta <= windowSize / 2 && isLocalMaximum; dTheta++)
                    {
                        for (int dR = -windowSize / 2; dR <= windowSize / 2; dR++)
                        {
                            if (dTheta == 0 && dR == 0) continue; // skip the center point

                            int newTheta = theta + dTheta;
                            int newR = r + dR;

                            if (newTheta >= 0 && newTheta < height && newR >= 0 && newR < width)
                            {
                                //when the surrounding pixel is bigger then the center value, the center is Not a local maximum
                                if (accumulator[newTheta, newR] > centerValue)
                                {
                                    isLocalMaximum = false;
                                    break;
                                }
                            }
                        }
                    }
                    if (isLocalMaximum)
                    {
                        nmsAccumulator[theta, r] = centerValue;
                    }
                    else
                    {
                        nmsAccumulator[theta, r] = 0;
                    }

                }
            }

            return nmsAccumulator;
        }



        /*
         * findPeaks: extract peaks from the accumulator above a threshold
         * input:   accumulator         hough accumulator
         *          threshold           minimum peak value
         *          diagonal            max rho value (diagonal length)
         *          thetaStepDegree     angular step for Hough transform
         *          rhoStep             radial step for Hough transform
         *          lowerAngleLimit     minimum angle limit
         * output:  list of detectedpeaks (rho, theta, value)
         */
        private List<(double rho, double thetaDegrees, int value)> findPeaks(int[,] accumulator, int threshold, double diagonal, float thetaStepDegree, float rhoStep, float lowerAngleLimit = 0f)
        {
            List<(double rho, double thetaDegrees, int value)> peaks = new List<(double rho, double thetaDegrees, int value)>();
            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);

            for (int thetaIndex = 0; thetaIndex < height; thetaIndex++)
            {
                for (int rhoIndex = 0; rhoIndex < width; rhoIndex++)
                {
                    int value = accumulator[thetaIndex, rhoIndex];
                    if (value >= threshold)
                    {
                        // convert indices to actual rho and theta values
                        double thetaDegrees = lowerAngleLimit + (thetaIndex * thetaStepDegree);
                        double rho = Math.Round((rhoIndex * rhoStep) - (diagonal / 2));


                        // ignore multiples of 45
                        if (Math.Abs(thetaDegrees % 90 - 45) > 1)
                        {
                            peaks.Add((rho, thetaDegrees, value));
                        }
                    }

                }
            }

            // sort peaks by value in descending order
            peaks.Sort((a, b) => b.value.CompareTo(a.value));

            return peaks;
        }


        // we make a Line segment object to use in the hougLineDetection function
        public struct LineSegment
        {

            public (int x, int y) Start; //This is a functional programming trick, this is just a Tuple
            public (int x, int y) End;

            public LineSegment((int, int) s, (int, int) e)
            {
                Start = s;
                End = e;
            }
        }


        /*
         * houghLineDetection: detect line segments using Hough transform
         * input:   image         byte array of the input image
         *          P             tuple containing rho and theta values
         *          minIntensity  minimum intensity for line detection
         *          minLen        minimum length of detected segments
         *          maxGap        maximum allowed gap between points in a segment
         * output:                list of detected line segments
         */
        public List<LineSegment> houghLineDetection(byte[,] image, (double rho, double theta) P, int minIntensity, int minLen, int maxGap)
        {
            int height = image.GetLength(0);
            int width = image.GetLength(1);
            List<LineSegment> segments = new List<LineSegment>();

            double thetaRadians = P.theta * Math.PI / 180.0; // radians
            double cosT = Math.Cos(thetaRadians);
            double sinT = Math.Sin(thetaRadians);

            List<(int, int)> currSeg = new List<(int, int)>();
            int segLen = 0;
            int gapCount = 0;

            if (Math.Abs(sinT) < 1e-10) // nearly vertical line
            {
                int x = (int)Math.Round(P.rho + width / 2); // adjust for center
                if (x >= 0 && x < width)
                {
                    for (int y = 0; y < height; y++)
                    {
                        processPoint(x, y);
                    }
                }
            }
            else
            {
                for (int x = 0; x < width; x++)
                {
                    double xCenter = x - width / 2;

                    int y = (int)Math.Round((P.rho - (xCenter) * cosT) / sinT + height / 2.0); // adjust for center

                    if (y >= 0 && y < height)
                    {
                        processPoint(x, y);
                    }
                }
            }

            if (segLen >= minLen && currSeg.Count > 0)
            {
                segments.Add(new LineSegment(currSeg[0], currSeg[currSeg.Count - 1]));
            }

            return segments;

            /*
             * processPoint: process a point to determine if it contributes to a line segment
             * input:   x          x-coordinate of the point
             *          y          y-coordinate of the point
             * output:             updates current segment or resets if conditions are not met
             */
            void processPoint(int x, int y)
            {
                byte pixVal = image[y, x];
                if (pixVal >= minIntensity)
                {
                    currSeg.Add((x, y));
                    segLen++;
                    gapCount = 0;
                }
                else
                {
                    gapCount++;
                    if (gapCount > maxGap)
                    {
                        if (segLen >= minLen)
                        {
                            segments.Add(new LineSegment(currSeg[0], currSeg[currSeg.Count - 1]));
                        }
                        currSeg.Clear();
                        segLen = 0;
                        gapCount = 0;
                    }
                }
            }
        }

        /*
         * detectAndDrawLineSegments: detect and draw line segments in a grayscale image using Hough transform
         * input:   inputImage          byte array of the input image
         *          thetaStepDegree     angle step size in degrees for Hough transform
         *          rhoStep             distance step size for Hough transform
         *          thresholdPercent    minimum peak threshold as a percentage of maximum
         *          minLineLength       minimum length of line segments to detect
         *          maxLineGap          maximum allowed gap between line segments
         *          minIntensity        minimum intensity for line detection
         *          lowerAngleLim       lower limit for angle (default = 0)
         *          upperAngleLim       upper limit for angle (default = 180)
         * output:                      byte array with detected line segments drawn
         */
        private byte[,] detectAndDrawLineSegments(byte[,] inputImage, float thetaStepDegree, float rhoStep,
                                                  float thresholdPercent, int minLineLength, int maxLineGap,
                                                  int minIntensity, float lowerAngleLim = 0f, float upperAngleLim = 180f)
        {
            int height = inputImage.GetLength(0);
            int width = inputImage.GetLength(1);


            // Step 1: Find peaks in the accumulator (hough transform included)
            List<(double r, double theta, int value)> peaks = peakFinding(inputImage, thresholdPercent, thetaStepDegree, rhoStep, lowerAngleLim, upperAngleLim);

            // Step 2: Detect line segments for each peak
            List<LineSegment> allLineSegments = new List<LineSegment>();
            foreach (var peak in peaks)
            {
                List<LineSegment> segments = houghLineDetection(inputImage, (peak.r, peak.theta), minIntensity, minLineLength, maxLineGap);
                allLineSegments.AddRange(segments);
            }

            // Step 3: Draw the detected line segments on a new image

            byte[,] outputImage = Preprocessor.reduceContrast(inputImage, 0.05);
            foreach (var segment in allLineSegments)
            {
                drawLineSegment(outputImage, segment, 255);
            }

            return outputImage;
        }

        /*
         * drawLineSegment: Draw a line segment on a grayscale image
         * input:   image          byte array of the image
         *          segment        lineSegment object defining the start and end points
         *          color          grayscale color value to draw the line
         */
        private void drawLineSegment(byte[,] image, LineSegment segment, byte color)
        {
            int x1 = segment.Start.x, y1 = segment.Start.y;
            int x2 = segment.End.x, y2 = segment.End.y;
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x1 >= 0 && x1 < image.GetLength(1) && y1 >= 0 && y1 < image.GetLength(0))
                {
                    image[y1, x1] = color;
                }

                if (x1 == x2 && y1 == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }


      


        /*
         * houghTransformAngleLimits: Perform the Hough transform on a binary or grayscale 
         *                            image, restricting angle detection to a specified range.
         * input:   inputImage          2D byte array of the input image.
         *          thetaStepDegree     Angle step size in degrees.
         *          rhoStep             Distance step size.
         *          lowerAngleLimit     Lower angle limit (0 to 180).
         *          upperAngleLimit     Upper angle limit (0 to 180).
         * output:                     2D int array representing the Hough accumulator.
         */
        private int[,] houghTransformAngleLimits(byte[,] inputImage, float thetaStepDegree, float rhoStep, float lowerAngleLimit, float upperAngleLimit)
        {
            int imgHeight = inputImage.GetLength(0);
            int imgWidth = inputImage.GetLength(1);
            if (thetaStepDegree <= 0 || rhoStep <= 0)
                throw new ArgumentException("step sizes must be positive.");
            if (lowerAngleLimit < 0 || upperAngleLimit > 180 || lowerAngleLimit >= upperAngleLimit)
            {
                Console.WriteLine($"{lowerAngleLimit} : {upperAngleLimit}");
                throw new ArgumentException("invalid angle limits.");
            }


            double diagonal = Math.Sqrt(imgWidth * imgWidth + imgHeight * imgHeight);
            int thetaCount = (int)((upperAngleLimit - lowerAngleLimit) / thetaStepDegree);
            int rhoCount = (int)(diagonal / rhoStep);
            bool binaryImage = BaseFunctions.isBinaryImage(inputImage);
            Console.WriteLine($"accumulator [{rhoCount},{thetaCount}], diagonal = {diagonal}, rhocount = {rhoCount}");

            int[,] accumulator = new int[thetaCount, rhoCount];
            double[] cosTheta = new double[thetaCount];
            double[] sinTheta = new double[thetaCount];

            for (int t = 0; t < thetaCount; t++)
            {
                double theta = ((lowerAngleLimit + t * thetaStepDegree) * Math.PI) / 180.0;
                cosTheta[t] = Math.Cos(theta);
                sinTheta[t] = Math.Sin(theta);
            }

            double centerY = imgHeight / 2.0;
            double centerX = imgWidth / 2.0;

            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    if (inputImage[y, x] > 0)
                    {
                        double cX = x - centerX;
                        double cY = y - centerY;
                        for (int t = 0; t < thetaCount; t++)
                        {
                            double rho = cX * cosTheta[t] + cY * sinTheta[t];
                            int rIndex = (int)Math.Round((rho + diagonal / 2) / rhoStep);
                            if (rIndex >= 0 && rIndex < rhoCount)
                            {
                                if (binaryImage)
                                {
                                    accumulator[t, rIndex]++;
                                }
                                else
                                {
                                    accumulator[t, rIndex] += inputImage[y, x];
                                }
                            }
                        }
                    }
                }
            }
            return accumulator;
        }




        // ----------------- choice tasks ---------------------



        /*
         * normaliseAccumulator: normalize the Hough transform accumulator 
         *                       to mitigate the bias towards longer lines 
         *                       and to facilitate a more balanced detection 
         *                       of line segments of varying lengths.
         * input:   accumulator        Hough transform accumulator.
         *          intputImg          image with which the accumulator was calculated
         *          thetaStep          step size for theta in degrees.
         *          rhoStep            step size for rho.
         * output:                     normalized accumulator as a 2D array of integers.
         */
        private int[,] normaliseAccumulator(int[,] accumulator, byte[,] inputImg, float thetastep, float rhoStep)
        {
            int width = inputImg.GetLength(1);
            int height = inputImg.GetLength(0);

            byte[,] whiteImage = new byte[height, width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    whiteImage[i, j] = 255; // Fully white image (all edges)
                }
            }

            bool binary = BaseFunctions.isBinaryImage(inputImg);

            int[,] maxHitsAccumulator = houghTransform(whiteImage, thetastep, rhoStep);

            for (int i = 0; i < accumulator.GetLength(0); i++)
            {
                for (int j = 0; j < accumulator.GetLength(1); j++)
                {
                    if (maxHitsAccumulator[i, j] > 0)
                    {
                        float normalizedValue = 0f;
                        if (binary)
                        {
                            // for visualisation purposes we scale by 255, otherwise with binary images, accumulator results will round to 0
                            normalizedValue = (float)(accumulator[i, j] * 255) / maxHitsAccumulator[i, j];
                        }
                        else
                        {
                            normalizedValue = (float)accumulator[i, j] / maxHitsAccumulator[i, j];
                        }

                        accumulator[i, j] = (int)normalizedValue;
                    }
                }
            }
            return accumulator;
        }


        // ------------------- circle detection ------------------
        public class Circle
        {
            public int CenterX { get; }
            public int CenterY { get; }
            public int Radius { get; }

            public Circle(int centerX, int centerY, int radius)
            {
                CenterX = centerX;
                CenterY = centerY;
                Radius = radius;
            }
        }


        private byte[,] DetectCircles(byte[,] inputImage, float minRadius, float maxRadius, float radiusStep, float thresholdPercentage)
        {
            int[,,] accumulator = houghTransformCircles(inputImage, minRadius, maxRadius, radiusStep);

            int[,,] supressedAccumulator = nonMaximumSuppression(accumulator);

            int maxAccumulatorValue = findMaxAccumulatorValue(accumulator);

            if (maxAccumulatorValue == 0)
            {
                return inputImage;
            }
            int thresholdValue = (int)Math.Round(thresholdPercentage * maxAccumulatorValue);
            List<Circle> result = findCircles(supressedAccumulator, minRadius, radiusStep, thresholdValue);
            Console.WriteLine("amount of circles detected: " + result.Count);
            return DrawCircles(result, inputImage);
        }

        private byte[,] DrawCircles(List<Circle> circles, byte[,] image)
        {
            byte[,] outputImage = Preprocessor.reduceContrast((byte[,])image.Clone(), 0.2); // Clone the original image to draw on

            foreach (Circle circle in circles)
            {
                DrawCircle(circle, outputImage);
            }

            return outputImage;
        }

        private void DrawCircle(Circle circle, byte[,] image)
        {
            int centerY = circle.CenterY;
            int centerX = circle.CenterX;
            int radius = circle.Radius;

            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius; // Starting decision parameter for Bresenham's algorithm

            while (y >= x)
            {
                DrawCirclePoints(centerX, centerY, x, y, image);
                x++;
                if (d > 0)
                {
                    y--;
                    d = d + 4 * (x - y) + 10;
                }
                else
                {
                    d = d + 4 * x + 6;
                }
            }
        }

        private void DrawCirclePoints(int centerX, int centerY, int x, int y, byte[,] image)
        {
            MarkPixel(centerX + x, centerY + y, image);
            MarkPixel(centerX - x, centerY + y, image);
            MarkPixel(centerX + x, centerY - y, image);
            MarkPixel(centerX - x, centerY - y, image);
            MarkPixel(centerX + y, centerY + x, image);
            MarkPixel(centerX - y, centerY + x, image);
            MarkPixel(centerX + y, centerY - x, image);
            MarkPixel(centerX - y, centerY - x, image);
        }

        private void MarkPixel(int x, int y, byte[,] image)
        {
            if (x >= 0 && x < image.GetLength(1) && y >= 0 && y < image.GetLength(0))
            {
                image[y, x] = 255; // Set pixel to white or your desired color for drawing
            }
        }


        private int findMaxAccumulatorValue(int[,,] accumulator)
        {
            int maxAccumulatorValue = 0;
            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);
            int radiusCount = accumulator.GetLength(2);

            for (int r = 0; r < radiusCount; r++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (accumulator[y, x, r] > maxAccumulatorValue)
                        {
                            maxAccumulatorValue = accumulator[y, x, r];
                        }
                    }
                }
            }

            return maxAccumulatorValue;
        }

        private int[,,] houghTransformCircles(byte[,] inputImage, float minRadius, float maxRadius, float radiusStep)
        {
            int imgHeight = inputImage.GetLength(0);
            int imgWidth = inputImage.GetLength(1);
            int radiusRange = (int)Math.Round((maxRadius - minRadius) / radiusStep + 1.0f);

            // accumulator to store votes: [centerY, centerX, radiusIndex]
            int[,,] accumulator = new int[imgHeight, imgWidth, radiusRange];


            for (int y = 0; y < imgHeight; y++)
            {
                for (int x = 0; x < imgWidth; x++)
                {
                    // check if  pixel is an edge pixel
                    if (inputImage[y, x] > 0)
                    {

                        for (int ri = 0; ri < radiusRange; ri++)
                        {

                            int radius = (int)(minRadius + ri * radiusStep);

                            // use Bresenham's algorithm to accumulate along the circle perimeter
                            bresenhamCircleAccumulateVotes(x, y, radius, accumulator, ri);
                        }
                    }
                }
            }

            return accumulator;
        }

        private List<Circle> findCircles(int[,,] accumulator, float minRadius, float radiusStep, int threshold)
        {
            List<Circle> detectedCircles = new List<Circle>();
            int width = accumulator.GetLength(0);
            int height = accumulator.GetLength(1);
            int radiusRange = accumulator.GetLength(2);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int r = 0; r < radiusRange; r++)
                    {
                        if (accumulator[x, y, r] >= threshold)
                        {
                            int radius = (int)(minRadius + r * radiusStep);
                            detectedCircles.Add(new Circle(y, x, radius));
                        }
                    }
                }
            }

            return detectedCircles;
        }

        private void bresenhamCircleAccumulateVotes(int centerX, int centerY, int radius, int[,,] accumulator, int radiusIndex)
        {
            int x = 0;
            int y = radius;
            int d = 3 - 2 * radius;  // starting decision parameter for Bresenhams algorithm


            bresenhamVoteForCircleCenter(centerX, centerY, x, y, accumulator, radiusIndex);

            // bresenham stopping condition
            while (y >= x)
            {
                x++;
                // update the decision parameter and adjust y
                if (d > 0)
                {
                    y--;
                    d = d + 4 * (x - y) + 10;
                }
                else
                {
                    d = d + 4 * x + 6;
                }
                bresenhamVoteForCircleCenter(centerX, centerY, x, y, accumulator, radiusIndex);
            }
        }

        // accumulate votes for potential circle centers using 8-way symmetry
        private void bresenhamVoteForCircleCenter(int cx, int cy, int x, int y, int[,,] accumulator, int radiusIndex)
        {
            // Accumulate votes for all eight points of the circle
            incrementAccumulator(cx + x, cy + y, accumulator, radiusIndex);
            incrementAccumulator(cx - x, cy + y, accumulator, radiusIndex);
            incrementAccumulator(cx + x, cy - y, accumulator, radiusIndex);
            incrementAccumulator(cx - x, cy - y, accumulator, radiusIndex);
            incrementAccumulator(cx + y, cy + x, accumulator, radiusIndex);
            incrementAccumulator(cx - y, cy + x, accumulator, radiusIndex);
            incrementAccumulator(cx + y, cy - x, accumulator, radiusIndex);
            incrementAccumulator(cx - y, cy - x, accumulator, radiusIndex);
        }

        // Increment the vote in the accumulator for valid points
        private void incrementAccumulator(int x, int y, int[,,] accumulator, int radiusIndex)
        {
            int height = accumulator.GetLength(0);
            int width = accumulator.GetLength(1);

            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                accumulator[y, x, radiusIndex]++;
            }
        }
        // Perform non-maximum suppression on the accumulator
        private int[,,] nonMaximumSuppression(int[,,] accumulator3D)
        {
            int height = accumulator3D.GetLength(0);
            int width = accumulator3D.GetLength(1);
            int radiusRange = accumulator3D.GetLength(2);

            int[,,] suppressedAccumulator = new int[height, width, radiusRange];

            // iterate through accumulator avoiding the border
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int r = 1; r < radiusRange - 1; r++)
                    {

                        int currentVote = accumulator3D[y, x, r];

                        bool isLocalMax = true;

                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                for (int dr = -1; dr <= 1; dr++)
                                {
                                    if (accumulator3D[y + dy, x + dx, r + dr] > currentVote)
                                    {
                                        isLocalMax = false;
                                        break;
                                    }
                                }
                                if (!isLocalMax) break;
                            }
                            if (!isLocalMax) break;
                        }

                        // If it's a local maximum, keep it in the suppressed accumulator
                        if (isLocalMax)
                        {
                            suppressedAccumulator[y, x, r] = currentVote;
                        }
                    }
                }
            }

            return suppressedAccumulator;
        }






        private List<(int x, int y, int radius)> findPeaks(int[,,] accumulator3D, int threshold)
        {
            int height = accumulator3D.GetLength(0);
            int width = accumulator3D.GetLength(1);
            int radiusRange = accumulator3D.GetLength(2);

            List<(int x, int y, int radius)> peaks = new List<(int x, int y, int radius)>();


            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int r = 0; r < radiusRange; r++)
                    {
                        if (accumulator3D[y, x, r] >= threshold)
                        {

                            int radius = (int)(r); // radius index
                            peaks.Add((x, y, radius));
                        }
                    }
                }
            }

            return peaks;
        }


        private int findMaxAccumulatorValue(int[,,] accumulator, int radiusRange, int imgHeight, int imgWidth)
        {
            int maxAccumulatorValue = 0;
            for (int r = 0; r < radiusRange; r++)
            {
                for (int y = 0; y < imgHeight; y++)
                {
                    for (int x = 0; x < imgWidth; x++)
                    {
                        if (accumulator[y, x, r] > maxAccumulatorValue)
                        {
                            maxAccumulatorValue = accumulator[y, x, r];
                        }
                    }
                }
            }
            return maxAccumulatorValue;
        }

        private void initValues()
        {
            // no parameter values in form yet to initialize
        }
    }
}
