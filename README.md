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
- [x] finding good parameters to analyse and decide when a region is be a card(shape) or not (see "findRegions")
- [x] analysing the outer contour of the region, or using convex hull or minBoundingBox 
- [x] Collecting a diverse dataset of card images (min 10, and 10 distractor images)
- [x] Rotating the card, and evtl scaling it 
- [x] using found Card-Shapes as ROI (Regions of interest) and doing analysis again
	- [x] another region finding
	- [ ] evtl. edge detection / contour shape or sth
	- [x] checking regions for (mean) color (black/red)
	- [x] region shape decides final verdict on which suit it is (hough circle detection)
	- [x] count these regions (evtl)




---

## Classes-Overview

The library provides several classes designed for specific image processing tasks:

- [**BaseFunctions**](#basefunctions): Core image processing operations.
- [**EdgeDetector**](#edgedetector): Edge detection algorithms, including the Canny edge detector.
- [**ImageConverter**](#imageconverter): Utilities for converting between image formats.
- [**Morphology**](#morphology): Handles morphological operations such as dilation and erosion.
- [**Preprocessor**](#preprocessor): Image preprocessing functions.
- [**Regions**](#regions): Methods for detecting and analyzing regions in binary images.
- [**BoundingShapeAnalyser**](#BoundingShapeAnalyser): methods for calculating minBoundingBox and its features

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


### BoundingShapeAnalyser

The `BoundingShapeAnalyser` class detects and analyzes oriented bounding boxes (OBBs) in images. It provides methods to compute the minimum bounding rectangle that can enclose a shape and offers tools to visualize and calculate features like aspect ratio and bounding-to-area ratio. Each bounding box is represented by the **OrientedBoundingBox** struct, which stores essential properties to characterize the bounding shape.

#### **OrientedBoundingBox Struct**
>This struct stores the characteristics of each identified bounding box.

- **Width**: The horizontal dimension of the bounding box.
- **Height**: The vertical dimension of the bounding box.
- **Angle**: The orientation angle of the bounding box in degrees.
- **Center**: The (Y, X) coordinates of the center of the bounding box.
- **Area**: The area of the bounding box, calculated as `Width × Height`.
- **AspectRatio**: The ratio between the longer and shorter side of the bounding box, computed as $\frac{\text{max(Width, Height)}}{\text{min(Width, Height)}}$.

#### **BoundingShapeAnalyser Class**

The `BoundingShapeAnalyser` class provides functions for detecting and analyzing oriented bounding boxes:

- **GetMinOBBox**: Determines the minimum area oriented bounding box that encloses a contour of points and returns an `OrientedBoundingBox`.
- **DrawMinAreaRect**: Draws the minimum bounding rectangle onto an image at the given orientation.
- **getAreaBoundingRatio**: Computes the ratio of the region's area to the bounding box's area, providing a measure of how tightly the region fits within its bounding box.

#### **Helper Functions**
- **DrawLine**: Draws a line between two points on the image using Bresenham's line algorithm.
- **RotatePoint**: Rotates a point by a given angle around the origin and returns the new coordinates.

[Back to top](#bounding-shape-analyser)
