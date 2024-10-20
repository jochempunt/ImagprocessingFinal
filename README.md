# Image Processing: The Final Assignment | Detecting a Playing Card

This project aims to detect specific playing card types, like spades or hearts, from an image taken at an above angle and  evtl. count the value of the cards (excluding face cards).

## Project Overview

### Our Idea
The project revolves around detecting specific playing card types, such as spades or hearts, placed on a surface (e.g., a table or the ground). The goal is to identify these cards from a somewhat overhead perspective and count their numerical value, excluding face cards.

### Plan
1. **Preprocessing**: Remove noise using Gaussian blur and adjust contrast to deal with lighting.
2. **Thresholding**: Apply basic thresholding (adaptive not required due to high contrast between white cards and background) and use morphological operations (e.g., opening/closing) to close small gaps.
3. **Region Finding and Labeling**: Identify regions and evaluate whether they resemble rectangles/cards based on properties like area, perimeter, circularity, centroids, and elongation.
4. **Region of Interest (ROI) Detection**: Use the identified regions as ROIs and apply further processing, like adaptive thresholding or edge detection (Sobel/Canny) to refine the analysis.
5. **Card Identification**: Analyze these regions to match specific shapes and colors for card suit recognition and count similar-sized shapes for card frequency.
6. **Output**: Generate the final image with the detected cards (of a specific type) bounded by boxes and display their values.

### Future Tools and Considerations
- Hough line or circle detection
- histogram functions,
- broad (Harris) corner detection.
- adjustive thresholding

### Tasks (not final/stay agile)
- [x] Cleaning up Code into seperate classes
- [x] Preprocessing (AND edge detection + morphology to try and close open edges)
- [x] Region Finding and analysing those shapes (can be expanded) 
- [ ] finding good parameters to analyse and decide when a region is be a card(shape) or not (see "findRegions")
- [ ] analysing the outer contour of the region (and maybe doing hough or corner detection if seems corner detec?)
- [ ] Collecting a diverse dataset of card images (min 10, and 10 distractor images)
- [ ] using found Card-Shapes as ROI (Regions of interest) and doing analysis again
	- [ ] another region finding
	- [ ] evtl. edge detection / contour shape or sth
	- [ ] checking regions for (mean) color (black/red)
	- [ ] region shape decides final verdict on which suit it is (matching shape or using region parameters)
	- [ ] count these regions (evtl)
- [ ] Discurring and think ab. angle coverage (considering rotations, perspective changes)
- [ ]  Discussingand think ab. potential card overlap coverage.




---

## Classes-Overview

The library provides several classes designed for specific image processing tasks:

- [**BaseFunctions**](#basefunctions): Core image processing operations.
- [**EdgeDetector**](#edgedetector): Edge detection algorithms, including the Canny edge detector.
- [**ImageConverter**](#imageconverter): Utilities for converting between image formats.
- [**Morphology**](#morphology): Handles morphological operations such as dilation and erosion.
- [**Preprocessor**](#preprocessor): Image preprocessing functions.
- [**Regions**](#regions): Methods for detecting and analyzing regions in binary images.

---

## Detailed Documentation
### ImageConverter

Utilities to convert images between different formats:

- **BitmapToGrayscale / GrayscaleToBitmap**: Convert between Bitmap and grayscale images.
- **BitmapToColor / ColorToBitmap**: Convert between Bitmap and color images.

[Back to top](#classes-overview)

---
### BaseFunctions

This class offers the core image processing methods, including convolution, pixel padding, and logical operations on binary images.

- **convolveImageSigned**: Performs convolution with a signed kernel, allowing negative values.
- **convolveImage**: Performs convolution with result clamping.
- **getPixelValueWithPadding**: Retrieves pixel values with applied padding for out-of-bounds coordinates.
- **andImages / orImages**: Logical AND/OR operations on two binary images.

[Back to top](#classes-overview)

---
### Preprocessor

This class includes various preprocessing methods:

- **reduceContrast**: Lowers the contrast in grayscale images.
- **InvertImage**: Inverts grayscale images.
- **adjustContrast**: Stretches intensity values to maximize contrast.
- **HistogramEqual**: Equalizes the image histogram.
- **applyGaussianFilter**: Applies Gaussian filtering for noise reduction.
- **applyMedianFilter**: Applies a median filter for smoothing.

[Back to top](#classes-overview)

---

### EdgeDetector

Contains methods to detect edges within an image:

- **getEdgeMagnitude**: Computes Sobel-based edge magnitudes (grayscale)
- **detectEdgesCanny**: Applies the Canny edge detection algorithm, allowing configuration of thresholds and sigma for Gaussian filtering. (returns binary edgemap)

[Back to top](#classes-overview)

---
### Morphology

This class provides morphological operations for binary and grayscale image

- **Dilate**: Performs dilation on an image using a specified kernel.
- **Erode**: Applies erosion to an image.
- **Open**: Executes morphological opening (erosion followed by dilation).
	- Removes small foreground details while preserving the overall shape of larger features.
- **Close**: Executes morphological closing (dilation followed by erosion).
	-  Fills small holes and gaps in foreground regions while preserving the overall shape.

[Back to top](#classes-overview)

---
### Regions

The `Regions` class detects and analyzes connected regions in binary images. It provides tools to label regions and compute important features that help distinguish them. Each region is represented by the **Region** struct, which stores key properties for analyzing the shape and position of the detected regions.

#### **Region Struct**
>This struct stores important characteristics of each identified region

- **Label**: Unique identifier for each region.
- **Pixels**: A list of coordinates making up the region.
- **OuterContour**: Coordinates outlining the region's outer boundary.
- **Area**: The total number of pixels within the region.
- **Perimeter**: The length of the region's boundary.
- **Circularity**: A measure of how circular the region is, calculated using $\frac{4\pi \times \text{Area}}{\text{Perimeter}^2}$
- **Central Moments**: Moments calculated from the region's pixel distribution, useful for characterizing its shape.
- **Centroid**: The center of mass, representing the average position of the pixels in the region.
- **Elongation**: The ratio between the major and minor axes, indicating the region's tendency to be elongated.

#### **Regions Class**

The `Regions` class provides functions for detecting, labeling, and analyzing regions:
- **floodFill**: fills  connected "edges" 
- **findConnectedRegions**: Identifies connected regions within a binary image and returns a list of `Region` structs.
- **calculateArea**: Calculates the area of a specific region.
- **calculatePerimeter**: Calculates the perimeter of a specific region.
- **detectCircularity**: Computes the circularity of a region.
- **findCentroid**: Determines the centroid of a region using its pixel distribution.
- **traceOuterContour**: Traces the outer contour of the detected region.
- **calculateElongation**: Evaluates the elongation (major-to-minor axis ratio) of a region.

[Back to top](#classes-overview)
