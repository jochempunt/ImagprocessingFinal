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
          Preprocess,
          FindEdges,
          FindRegions
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
                Color[,] workingImage = ImageConverter.BitmapToColor(InputImage);
                Color[,] processedImage = ProcessImage(workingImage);
                OutputImage = ImageConverter.ColorToBitmap(processedImage);
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
        private Color[,] ProcessImage(Color[,] workingImage)
        {
            //Plan:
            // 1. Preprocessing (Get rid of noise, adjust contrast, deal with ligthing
            // 2. Thresholding (so far not adaptive) and using morphology to close the smalles gaps
            // 3. region finding, and working out which are more likely to be rectangles/cards
            // 4. using the found regions as ROIs (Regions of interest) and thresholding (adaptive) again evtl edge detections
            // 5. checking these regions  evtl. if they match specific shapes, colors, and counting the "same size" ones if frequent.
            // 6. outputting final image with cards (of a specific type) in a bounding box, and evtl. output their card value

            // tools we havent considered yet but could use: hough line or circle detection, histogramm functions, broad (harris) corner detection...
            // things we´still need to do: collect images (at least ten maybe more) and distractor images
            // clear which angles we want to cover (rotation/perspective wise) and if cards are partially overlayd by sth etc.
            // write consize report on this (4-5 pages)
            int height = workingImage.GetLength(0);
            int width = workingImage.GetLength(1);
            byte[,] grayScale = ImageConverter.ToGrayscale(workingImage);
            switch ((ProcessingFunctions)comboBox.SelectedIndex)
            {
                case ProcessingFunctions.Preprocess:
                    byte[,] adjustedContrast = Preprocessor.adjustContrast(grayScale);
                    byte[,] blurred = Preprocessor.applyGaussianFilter(adjustedContrast, 1f, 3);
                    byte[,] thresholded = Preprocessor.thresholdImage(blurred, 127);
                    byte[,] morphed = Morphology.Close(thresholded, Morphology.ElementShape.Plus, 7, Morphology.ImageType.Binary);
                    return ImageConverter.ToColorImage(morphed);
                case ProcessingFunctions.FindEdges:
                    return  ImageConverter.ToColorImage(EdgeDetector.detectEdgesCanny(grayScale, 200, 250, 1f, 5));
                case ProcessingFunctions.FindRegions:
                    List<Region> regions =  Regions.FindRegions(grayScale);
                    Console.WriteLine($"Total number of regions: {regions.Count}");

                    foreach(Region region in regions)
                    {
                        if(region.Area >= 200)
                        {
                            Console.WriteLine($"Region {region.Label} Area: {region.Area} Perimeter: {region.Perimeter}");
                            Console.WriteLine($"| Circularity: {region.Circularity} Elongation: {region.Elongation} Centroid: {region.Centroid}");
                            Console.WriteLine($"---------------------------------");
                        }
                    }
                    return Regions.DrawRegions(regions,height,width);
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
        private void INFOIBV_Load(object sender, EventArgs e)
        {

        }
        private void initValues()
        {
            // no parameter values in form yet to initialize
        }
    }
}
