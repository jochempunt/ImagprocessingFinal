using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.IO;
using static INFOIBV.CircleDetectorHough;


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
        private Byte[,] Template;
        private Bitmap InputImage;
        private Bitmap OutputImage;

        /*
         * this enum defines the processing functions that will be shown in the dropdown (a.k.a. combobox)
         * you can expand it by adding new entries to applyProcessingFunction()
         */
        private enum ProcessingFunctions
        {
            Preprocess,
            FindEdges,
            DetectCircles,
            CheckColor,
            Scale_Image,
            FindHeartCards
        }




        #region Global Parameters
        // preprocessing - edge detection and morphology
        byte LOW_CANNY_THRESHOLD = 50;
        byte HIGH_CANNY_THRESHOLD = 150;
        float SIGMA_GAUSSIAN_CANNY = 1.4f;
        int KERNEL_SIZE_GAUSSIAN_CANNY = 5;
        int EDGE_CONNECTING_KERNELSIZE = 3;
        int REGION_CLEANING_KERNELSIZE = 9;

        //Card detection
        float MIN_AREA_SCALE_CARD = 0.01f;
        float MAX_AREA_SCALE_CARD = 0.3f; 
        float MIN_ELONGATION_CARD = 1.15f;
        float MAX_ELONGATION_CARD = 1.7f;
        float MIN_CIRCULARITY_CARD = 0.45f;
        float MAX_CIRCULARITY_CARD = 0.85f;
        float MIN_ASPECTRATIO_CARD = 1.1f;
        float MAX_ASPECTRATIO_CARD = 1.7f;

        float MIN_RECTANGULARITY_CARD = 0.8f; // area / bounding box area = rectangularity
        int MIN_WIDTH_CARD_PX = 300;

        // refinement
        byte SYMBOL_THRESHOLD = 126;
        float MIN_HEART_SCALE = 0.005f;
        float MAX_HEART_SCALE = 0.7f;
        int MIN_HEARTBOX_AREA_PX = 200;
        float MIN_SYMBOL_AREA_RATIO = 0.4f;



        #endregion





        public INFOIBV()
        {
            InitializeComponent();
            populateCombobox();
            //imageProcessor = new ImageProcessor();
            initValues();
            LoadTemplate();
        }


        private void LoadTemplate()
        {
            // Go up two levels from bin\Debug to reach project root
            string projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\.."));
            string fullPath = Path.Combine(projectDir, "images", "invertedTemplate.bmp");

            Console.WriteLine($"Loading from: {fullPath}"); // Debug line

            try
            {
                using (var templateBitmap = new Bitmap(fullPath))
                {
                    Template = ImageConverter.BitmapToGrayscale(templateBitmap);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
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
                    InputImage.Size.Height > 1500 || InputImage.Size.Width > 1500) // dimension check (may be removed or altered)
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 1500)");
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
                Color[,] workingImage = ImageConverter.BitmapToColor(InputImage);
                Color[,] processedImage = ProcessImage(workingImage);
                OutputImage = ImageConverter.ColorToBitmap(processedImage);
                pictureBox2.Image = OutputImage;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
            }
        }

        /*
         * applyProcessingFunction: defines behavior of function calls when "Apply" is pressed
         */
        private Color[,] ProcessImage(Color[,] workingImage)
        {
            int height = workingImage.GetLength(0);
            int width = workingImage.GetLength(1);
            byte[,] grayScale = ImageConverter.ToGrayscale(workingImage);


            switch ((ProcessingFunctions)comboBox.SelectedIndex)
            {
                case ProcessingFunctions.Preprocess:

                    byte[,] adjustedContrast = Preprocessor.adjustContrast(grayScale);
                    //adjustedContrast = Preprocessor.applyGaussianFilter(grayScale,2f,5);


                    //byte[,] blurred = Preprocessor.applyGaussianFilter(grayScale, 2f, 13);
                    byte[,] edges = EdgeDetector.detectEdgesCanny(adjustedContrast, 40, 150, 1.5f, 7);


                    // 2. Dilate edges slightly to connect small gaps
                    byte[,] dilatedEdges = Morphology.Dilate(edges, Morphology.ElementShape.Plus, 3, Morphology.ImageType.Binary);

                    // 3. Close edges to connect larger gaps
                    byte[,] closedEdges = Morphology.Close(dilatedEdges, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary);

                    // do a floodFill to fill out connected edes into regions
                    byte[,] regionsBinary = Regions.FloodFillSolid(closedEdges);
                    // clean up noise and fill small holes
                    byte[,] cleaned = Morphology.Open(regionsBinary, Morphology.ElementShape.Square, 11, Morphology.ImageType.Binary);
                    return ImageConverter.ToColorImage(cleaned);
                case ProcessingFunctions.FindEdges:
                    byte[,] adjj = Preprocessor.adjustContrast(grayScale);

                    byte[,] edger = EdgeDetector.detectEdgesCanny(adjj, 50, 150, 1.5f, 7);
                    // 2. Dilate edges slightly to connect small gaps
                    byte[,] dilateEdger = Morphology.Dilate(edger, Morphology.ElementShape.Plus, 5, Morphology.ImageType.Binary);

                    // 3. Close edges to connect larger gaps

                    return ImageConverter.ToColorImage(Morphology.Close(dilateEdger, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary));
                case ProcessingFunctions.FindHeartCards:
                    byte[,] adj = Preprocessor.adjustContrast(grayScale);

                    
                    byte[,] edge = EdgeDetector.detectEdgesCanny(adj, 50, 150, 1.4f, 5);

                    // 2. Dilate edges slightly to connect small gaps
                    byte[,] dilateEdge = Morphology.Dilate(edge, Morphology.ElementShape.Plus, 3, Morphology.ImageType.Binary);

                    // 3. Close edges to connect larger gaps
                    byte[,] closedEdge = Morphology.Close(dilateEdge, Morphology.ElementShape.Square, 3, Morphology.ImageType.Binary);

                    // do a floodFill to fill out connected edes into regions
                    byte[,] solidRegions = Regions.FloodFillSolid(closedEdge);

                    // clean up noise and fill small holes
                    byte[,] cleanedRegions = Morphology.Open(solidRegions, Morphology.ElementShape.Plus, 9, Morphology.ImageType.Binary);
                    List<Region> regions = Regions.FindRegions(cleanedRegions);
                    Console.WriteLine($"Total number of regions: {regions.Count}");

                    int imageArea = (width * height);

                    Console.WriteLine("min area: " +  imageArea *MIN_AREA_SCALE_CARD);
                    Console.WriteLine("max area: " + imageArea  *MAX_AREA_SCALE_CARD);

                    foreach (Region region in regions)
                    {
                        if (region.Area >= imageArea * MIN_AREA_SCALE_CARD && region.Area <= imageArea * MIN_AREA_SCALE_CARD)
                        {
                            Console.WriteLine($"Region {region.Label} Area: {region.Area} Perimeter: {region.Perimeter}");
                            Console.WriteLine($"| Circularity: {region.Circularity} Elongation: {region.Elongation} Centroid: {region.Centroid}");
                            Console.WriteLine($"---------------------------------");
                        }


                    }

                    // Filter regions based on card-like properties
                    List<Region> potentialCardRegions = regions.Where(r =>
                        r.Area >= imageArea* MIN_AREA_SCALE_CARD &&           // min  size
                        r.Area <= imageArea * MAX_AREA_SCALE_CARD &&         // max size
                        r.Elongation > MIN_ELONGATION_CARD &&               // cards are rectangular (so elongated)
                        r.Elongation < MAX_ELONGATION_CARD &&              // Not too elongated
                        r.Circularity > MIN_CIRCULARITY_CARD &&
                        r.Circularity < MAX_CIRCULARITY_CARD             // Reasonably rectangular shape

                    ).ToList();
                    Console.WriteLine(" found card shapes: " + potentialCardRegions.Count);

                    Color[,] cardRegionImg = Regions.DrawRegions(potentialCardRegions, height, width);



                    // refinement
                    List<DetectedCard> detectedCards = new List<DetectedCard>();

                    for (int i = 0; i < potentialCardRegions.Count; i++)
                    {
                        OrientedBoundingBox minBoundingBox = BoundingShapeAnalyser.GetMinOBBox(potentialCardRegions[i].OuterContour);
                        double areaBoundingRatio = BoundingShapeAnalyser.getAreaBoundingRatio(minBoundingBox, potentialCardRegions[i].Area);
                        Console.WriteLine($"Region: {potentialCardRegions[i].Label}, Bounding Box Ratio: {areaBoundingRatio}");
                        Console.WriteLine($"angle: {minBoundingBox.Angle}, {minBoundingBox.AspectRatio}");

                        if (areaBoundingRatio > MIN_RECTANGULARITY_CARD)
                        {
                            double aspectR = Math.Round(minBoundingBox.AspectRatio, 2);
                            if (aspectR > MIN_ASPECTRATIO_CARD && aspectR <= MAX_ASPECTRATIO_CARD)
                            {
                                Color[,] colorCardUpright = BoundingShapeAnalyser.RotateRegionToUpright(workingImage, minBoundingBox);
                                double cardRatio = ((double)colorCardUpright.GetLength(1)) / colorCardUpright.GetLength(0);

                                if (colorCardUpright.GetLength(1) < MIN_WIDTH_CARD_PX)
                                {
                                    Console.WriteLine("---rescaled card--- ");
                                    colorCardUpright = BoundingShapeAnalyser.ScaleImageBilinear(colorCardUpright, MIN_WIDTH_CARD_PX, (int)(MIN_WIDTH_CARD_PX * cardRatio));
                                    // return ImageConverter.ToColorImage(grayscaleCardUpright);
                                }
                                byte[,] grayscaleCardUpright = ImageConverter.ToGrayscale(colorCardUpright);


                                // Create thresholded version
                                byte[,] thresholdedCardRegion = Preprocessor.thresholdImage(grayscaleCardUpright, SYMBOL_THRESHOLD);


                                thresholdedCardRegion = Preprocessor.InvertImage(thresholdedCardRegion);

                                detectedCards.Add(new DetectedCard
                                {
                                    Region = potentialCardRegions[i],
                                    BoundingBox = minBoundingBox,
                                    ColorImage = colorCardUpright,
                                    GrayscaleImage = grayscaleCardUpright,
                                    ThresholdedImage = thresholdedCardRegion,
                                });
                            }
                        }
                    }

                    for (int cardIndex = 0; cardIndex < detectedCards.Count; cardIndex++)
                    {
                        var card = detectedCards[cardIndex];

                        List<Region> potentialSymbols = Regions.FindRegions(card.ThresholdedImage);

                        Console.WriteLine($"Card {cardIndex}: Found {potentialSymbols.Count} symbols");

                        for (int symbolIndex = 0; symbolIndex < potentialSymbols.Count; symbolIndex++)
                        {
                            Region pSymbol = potentialSymbols[symbolIndex];
                            AxisAlignedBoundingBox aabb = BoundingShapeAnalyser.GetAABB(pSymbol.OuterContour);
                            Console.WriteLine($"Card {cardIndex}, Symbol {symbolIndex}: Width={aabb.Width}, Height={aabb.Height}, Area={aabb.Area}");

                            if (aabb.Area < card.Region.Area * MAX_HEART_SCALE &&
                                aabb.Area > card.Region.Area * MIN_HEART_SCALE &&
                                aabb.Area > MIN_HEARTBOX_AREA_PX)
                            {
                                double area_filled = pSymbol.Area / aabb.Area;
                                Console.WriteLine($"Card {cardIndex}, Symbol {symbolIndex} --> filled area = {area_filled}");


                                if (area_filled > MIN_SYMBOL_AREA_RATIO)
                                {

                                    if (ColorComparison.isRedColor(pSymbol, card.ColorImage))
                                    {
                                        Console.WriteLine("!!symbol is red!!");
                                        byte[,] boundBoxContent = BoundingShapeAnalyser.ExtractAABBContent(card.GrayscaleImage, aabb);
                                        List<CircleDetectorHough.Circle> circles = CircleDetectorHough.findCircles(boundBoxContent);
                                        List<CircleDetectorHough.Circle> sortedCircles = circles.OrderBy(c => c.CenterY).ToList();

                                        for (int circleIndex = 0; circleIndex < sortedCircles.Count; circleIndex++)
                                        {
                                            CircleDetectorHough.Circle circle = sortedCircles[circleIndex];
                                            Console.WriteLine($"Card {cardIndex}, Symbol {symbolIndex}, Circle {circleIndex} -> position: {circle.CenterY} y, {circle.CenterX} x");
                                        }


                                        if (sortedCircles.Count > 0 && heartShapeDetector.isHeartBasedOnCircles(aabb, sortedCircles))
                                        {
                                            card.Symbols.Add(pSymbol);
                                            detectedCards[cardIndex].HasHeart = true;  // Update the card in the list
                                        }
                                    }


                                }
                            }
                        }


                    }
                    foreach (DetectedCard detectedCard in detectedCards)
                    {
                        Color boxColor = detectedCard.HasHeart ? Color.Red : Color.Blue; // Blue with 40% opacity

                        int thickness = 2;
                        // Draw the bounding box on the working image
                        BoundingShapeAnalyser.DrawBoundingBox(workingImage, detectedCard.BoundingBox, boxColor, thickness);
                    }
                    //Console.WriteLine($"amount of heart cards detected = {heartCards.Count}");
                    return workingImage;
                case ProcessingFunctions.DetectCircles:
                    List<CircleDetectorHough.Circle> foundCircles = CircleDetectorHough.findCircles(grayScale);
                    return CircleDetectorHough.DrawCircles(foundCircles, workingImage);
                case ProcessingFunctions.CheckColor:
                    byte[,] thresholded = Preprocessor.thresholdImage(grayScale, 127);
                    thresholded = Preprocessor.InvertImage(thresholded);
                    List<Region> regionz = Regions.FindRegions(thresholded);
                    foreach (Region region in regionz)
                    {
                        Console.WriteLine($"region {region.Label} is Red: {ColorComparison.isRedColor(region, workingImage)}");
                    }
                    return Regions.DrawRegions(regionz, height, width);
                case ProcessingFunctions.Scale_Image:
                    int newWidth = 100;


                    // Calculate new height while preserving aspect ratio
                    int newHeight = (int)((double)height / width * newWidth);
                    if (width < newWidth)
                    {
                        return BoundingShapeAnalyser.ScaleImageBilinear(workingImage, newWidth, newHeight);
                    }
                    else
                        return workingImage;
                default:
                    return null;
            }
        }



        public class DetectedCard  // Changed from struct to class
        {
            public Region Region { get; set; }
            public OrientedBoundingBox BoundingBox { get; set; }
            public Color[,] ColorImage { get; set; }
            public byte[,] GrayscaleImage { get; set; }
            public byte[,] ThresholdedImage { get; set; }
            public List<Region> Symbols { get; set; }
            public bool HasHeart { get; set; }

            public DetectedCard()
            {
                Symbols = new List<Region>();
                HasHeart = false;
            }
        }


        private static byte FindGlobalMin(byte[,] array)
        {
            byte min = byte.MaxValue;
            for (int y = 0; y < array.GetLength(0); y++)
                for (int x = 0; x < array.GetLength(1); x++)
                    if (array[y, x] < min) min = array[y, x];
            return min;
        }

        private static byte FindGlobalMax(byte[,] array)
        {
            byte max = byte.MinValue;
            for (int y = 0; y < array.GetLength(0); y++)
                for (int x = 0; x < array.GetLength(1); x++)
                    if (array[y, x] > max) max = array[y, x];
            return max;
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
        private void INFOIBV_Load(object sender, EventArgs e)
        {

        }
        private void initValues()
        {
            // no parameter values in form yet to initialize
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
