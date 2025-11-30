using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public class MapGenerator
    {
        private Color Background = Color.LightGray;

        /// <summary>
        /// The current scaling in pixels per meter
        /// </summary>
        public double CurrentScaleFactor { get; private set; }
        public Field CurrentField { get; private set; }

        /// <summary>
        /// Offset of tractor Y position from top of screen in range 0 -> 1 where 0.5 is middle of screen
        /// </summary>
        public double TractorYOffset = 0.7;

        /*public Bitmap GenerateZoomToFit
            (
            Field Field,
            int CanvasWidth,
            int CanvasHeight,
            bool ShowGrid
            )
        {
            if (Field.Bins.Count == 0)
            {
                throw new InvalidOperationException("No bin data available for map generation");
            }

            // Calculate grid dimensions
            var bins = Field.Bins;
            var minX = bins.Min(b => b.X);
            var maxX = bins.Max(b => b.X);
            var minY = bins.Min(b => b.Y);
            var maxY = bins.Max(b => b.Y);

            var gridWidth = maxX - minX + 1;
            var gridHeight = maxY - minY + 1;

            // Calculate image dimensions
            double ScaleFactor = CanvasWidth / gridWidth;

            return Generate(Field, ShowGrid, ScaleFactor);
        }*/


        /// <summary>
        /// Generates the map as a bitmap
        /// </summary>
        /// <param name="Field">Field to display</param>
        /// <param name="Width">Width of bitmap</param>
        /// <param name="Height">Height of bitmap</param>
        /// <param name="ShowGrid">true to show the bin grid</param>
        /// <param name="ScaleFactor">Zoom level for map in pixels per m</param>
        /// <param name="TractorLat">Tractor latitude</param>
        /// <param name="TractorLon">Tractor longitude</param>
        /// <param name="TractorHeading">Heading of tractor in degrees</param>
        /// <returns>Generated bitmap</returns>
        public Bitmap Generate
            (
            Field Field,
            int ImageWidthpx,
            int ImageHeightpx,
            bool ShowGrid,
            double ScaleFactor,
            double TractorLat,
            double TractorLon,
            double TractorHeading
            )
        {
            if (Field.Bins.Count == 0)
            {
                throw new InvalidOperationException("No bin data available for map generation");
            }

            CurrentField = Field;
            CurrentScaleFactor = ScaleFactor;

            // Create bitmap directly in memory
            Bitmap bitmap = new Bitmap(ImageWidthpx, ImageHeightpx, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.FillRectangle(new SolidBrush(Color.LightGray), 0, 0, bitmap.Width, bitmap.Height);
            }

            // get size of display in meters
            double ImageWidthM = ImageWidthpx * ScaleFactor;
            double ImageHeightM = ImageHeightpx * ScaleFactor;

            // get size of map in meters
            double MapWidthM = Field.FieldMaxX - Field.FieldMinX;
            double MapHeightM = Field.FieldMaxY - Field.FieldMinY;

            // get size of map in pixels
            int MapWidthpx = (int)Math.Round(MapWidthM * ScaleFactor);
            int MapHeightpx = (int)Math.Round(MapHeightM * ScaleFactor);

            // calculate location of tractor in pixels
            int TractorXpx = ImageWidthpx / 2;
            int TractorYpx = (int)(ImageHeightpx * TractorYOffset);

            // Calculate grid dimensions
            var bins = Field.Bins;
            var minX = bins.Min(b => b.X);
            var maxX = bins.Max(b => b.X);
            var minY = bins.Min(b => b.Y);
            var maxY = bins.Max(b => b.Y);

            var gridWidth = maxX - minX + 1;
            var gridHeight = maxY - minY + 1;

            var elevationGrid = CreateElevationGrid(bins, minX, maxX, minY, maxY, gridWidth, gridHeight);
            
            // The bin origin is the field coordinate that corresponds to bin grid index 0
            // Bins are created with: BinX = Floor((Point.X - MinX) / BinSizeM)
            // where MinX is the minimum X of topology points, which should equal Field.FieldMinX
            // So bin grid index 0 corresponds to field coordinate Field.FieldMinX
            double binOriginX = Field.FieldMinX;
            double binOriginY = Field.FieldMinY;

            double[] ElevationRange = CalculateInitialElevationRange(Field);
            double minElevation = ElevationRange[0];
            double maxElevation = ElevationRange[1];

            if (minElevation == 0.0 && maxElevation == 0.0)
            {
                throw new InvalidOperationException("No valid non-zero elevation data found in initial elevation range");
            }

            // If elevation range is too small, add some artificial range for visualization
            if (maxElevation - minElevation < 0.01) // Less than 1cm difference
            {
                var centerElevation = (minElevation + maxElevation) / 2;
                minElevation = centerElevation - 0.1; // 10cm below center
                maxElevation = centerElevation + 0.1; // 10cm above center
            }

            // Generate color palette
            var colorPalette = GenerateColorPalette();

            // get top left corner of map inside image
            Point MapTopLeft = GetMapOffset(TractorLat, TractorLon, TractorXpx, TractorYpx);

            // get top left corner of map inside image
            // we align the tractor location with the location on the map
            //int MapLeftpx = (ImageWidthpx - MapWidthpx) / 2;
            //int MapToppx = (ImageHeightpx - MapHeightpx) / 2;
            int MapLeftpx = MapTopLeft.X;
            int MapToppx = MapTopLeft.Y;

            // Lock bitmap data for direct memory access
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(MapLeftpx, MapToppx, MapWidthpx, MapHeightpx),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int stride = bitmapData.Stride;
                int bytesPerRow = MapWidthpx * 3; // Actual bytes needed for one row of the locked rectangle
                
                // Allocate buffer for one row of pixel data (BGR format)
                byte[] rowData = new byte[bytesPerRow];

                // Write pixel data directly to bitmap memory (rows stored bottom-to-top in BMP)
                for (int y = 0; y < MapHeightpx; y++)
                {
                    // BMP format stores rows bottom-to-top, so we need to flip Y
                    int bmpY = MapHeightpx - 1 - y;
                    int rowOffset = 0;

                    for (int x = 0; x < MapWidthpx; x++)
                    {
                        // convert pixel coordinates to bin grid indices
                        Point BinPoint = PixelToBin(x, y);

                        // Convert bin grid indices to array indices (elevationGrid uses 0-based indexing)
                        int gridX = BinPoint.X - minX;
                        int gridY = BinPoint.Y - minY;

                        byte r, g, b;

                        if (gridX < gridWidth && gridY < gridHeight)
                        {
                            var elevation = elevationGrid[gridY, gridX];

                            if (elevation.HasValue && elevation.Value != 0.0)
                            {
                                // Normalize elevation to 0-255 range
                                var normalizedElevation = (elevation.Value - minElevation) / (maxElevation - minElevation);
                                var colorIndex = Math.Max(0, Math.Min(255, (int)(normalizedElevation * 255)));

                                // Apply color from palette (palette is RGB, bitmap needs BGR)
                                r = (byte)colorPalette[colorIndex, 0]; // Red
                                g = (byte)colorPalette[colorIndex, 1]; // Green
                                b = (byte)colorPalette[colorIndex, 2]; // Blue
                            }
                            else
                            {
                                // No data
                                r = Background.R;
                                g = Background.G;
                                b = Background.B;
                            }
                        }
                        else
                        {
                            // outside grid
                            r = Background.R;
                            g = Background.G;
                            b = Background.B;
                        }

                        // Draw grid lines
                        if (ShowGrid)
                        {
                            if ((x % ScaleFactor == 0) || (y % ScaleFactor == 0))
                            {
                                r = g = b = 0x40; // Dark gray
                            }
                        }

                        // Write BGR (bitmap format)
                        rowData[rowOffset + 0] = b; // Blue
                        rowData[rowOffset + 1] = g; // Green
                        rowData[rowOffset + 2] = r; // Red
                        rowOffset += 3;
                    }

                    // Copy row data to bitmap memory
                    // Scan0 points to the locked rectangle, stride is the full bitmap stride
                    IntPtr rowPtr = new IntPtr(bitmapData.Scan0.ToInt64() + (bmpY * stride));
                    Marshal.Copy(rowData, 0, rowPtr, bytesPerRow);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            Decorate(bitmap);

            return bitmap;
        }

        private void Decorate
            (
            Bitmap Map
            )
        {
            using (Graphics g = Graphics.FromImage(Map))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // draw tractor
                int TractorXpx = Map.Width / 2;
                int TractorYpx = (int)(Map.Height * TractorYOffset);
                g.DrawImage(Properties.Resources.navarrow_256px, TractorXpx - 24, TractorYpx - 24, 48, 48);
            }
        }

        /// <summary>
        /// Gets the top left location of the map inside the image
        /// </summary>
        /// <param name="TractorLat">Current tractor latitude</param>
        /// <param name="TractorLon">Current tractor longitude</param>
        /// <param name="TractorXpx">Current tractor X location in image in pixels</param>
        /// <param name="TractorYpx">Current tractor Y location in image in pixels</param>
        /// <returns>Top-left corner of the map in image pixel coordinates</returns>
        private Point GetMapOffset
            (
            double TractorLat,
            double TractorLon,
            int TractorXpx,
            int TractorYpx
            )
        {
            // Convert tractor's lat/lon to UTM coordinates (field coordinates in meters)
            UTM.UTMCoordinate TractorUTM = UTM.FromLatLon(TractorLat, TractorLon);
            double TractorFieldX = TractorUTM.Easting;
            double TractorFieldY = TractorUTM.Northing;

            // Convert tractor's field coordinates to pixel coordinates relative to the map (0,0 is top-left of map)
            Point TractorFieldPx = FieldMToPixel(TractorFieldX, TractorFieldY);

            // Calculate the offset so that the tractor's field position aligns with the fixed pixel position
            // MapTopLeft = TractorImagePosition - TractorFieldPosition
            int MapLeftpx = TractorXpx - TractorFieldPx.X;
            int MapToppx = TractorYpx - TractorFieldPx.Y;

            return new Point(MapLeftpx, MapToppx);
        }

        /// <summary>
        /// Convert pixel coordinates to field coordinate in meters
        /// </summary>
        /// <param name="PixelX">Pixel X coordinate</param>
        /// <param name="PixelY">Pixel Y coordinate</param>
        /// <returns></returns>
        private PointD PixelToFieldM
            (
            int PixelX,
            int PixelY
            )
        {
            double FieldX = CurrentField.FieldMinX + (PixelX / CurrentScaleFactor);
            double FieldY = CurrentField.FieldMinY + (PixelY / CurrentScaleFactor);

            return new PointD(FieldX, FieldY);
        }

        /// <summary>
        /// Convert field coordinate in meters to bin grid indices
        /// </summary>
        /// <param name="FieldX">Field X coordinate in meters</param>
        /// <param name="FieldY">Field Y coordinate in meters</param>
        /// <returns>Bin grid X and Y</returns>
        private Point FieldMToBin
            (
            double FieldX,
            double FieldY
            )
        {
            int binGridX = (int)Math.Floor((FieldX - CurrentField.FieldMinX) / Field.BIN_SIZE_M);
            int binGridY = (int)Math.Floor((FieldY - CurrentField.FieldMinY) / Field.BIN_SIZE_M);

            return new Point(binGridX, binGridY);
        }

        /// <summary>
        /// Converts pixel coordinates to bin grid indices
        /// </summary>
        /// <param name="PixelX">Pixel X coordinate</param>
        /// <param name="PixelY">Pixel Y coordinate</param>
        /// <returns>Bin grid X and Y</returns>
        private Point PixelToBin
            (
            int PixelX,
            int PixelY
            )
        {
            PointD FieldPoint = PixelToFieldM(PixelX, PixelY);
            return FieldMToBin(FieldPoint.X, FieldPoint.Y);
        }

        /// <summary>
        /// Converts bin grid indices to pixel coordinates
        /// </summary>
        /// <param name="BinGridX">Bin grid X index</param>
        /// <param name="BinGridY">Bin grid Y index</param>
        /// <returns>Pixel X and Y coordinates</returns>
        private Point BinToPixel
            (
            int BinGridX,
            int BinGridY
            )
        {
            PointD FieldPoint = BinToFieldM(BinGridX, BinGridY);
            return FieldMToPixel(FieldPoint.X, FieldPoint.Y);
        }

        /// <summary>
        /// Convert field coordinate in meters to pixel coordinates
        /// </summary>
        /// <param name="FieldX">Field X coordinate in meters</param>
        /// <param name="FieldY">Field Y coordinate in meters</param>
        /// <returns>Pixel X and Y coordinates</returns>
        private Point FieldMToPixel
            (
            double FieldX,
            double FieldY
            )
        {
            int PixelX = (int)Math.Round((FieldX - CurrentField.FieldMinX) * CurrentScaleFactor);
            int PixelY = (int)Math.Round((FieldY - CurrentField.FieldMinY) * CurrentScaleFactor);

            return new Point(PixelX, PixelY);
        }

        /// <summary>
        /// Convert bin grid indices to field coordinate in meters
        /// </summary>
        /// <param name="BinGridX">Bin grid X index</param>
        /// <param name="BinGridY">Bin grid Y index</param>
        /// <returns>Field X and Y coordinates in meters</returns>
        private PointD BinToFieldM
            (
            int BinGridX,
            int BinGridY
            )
        {
            double FieldX = CurrentField.FieldMinX + (BinGridX * Field.BIN_SIZE_M);
            double FieldY = CurrentField.FieldMinY + (BinGridY * Field.BIN_SIZE_M);

            return new PointD(FieldX, FieldY);
        }

        // Calculate and set the initial elevation range (ignoring zero elevations)
        private double[] CalculateInitialElevationRange
            (
            Field Field
            )
        {
            double InitialMinimumElevationM;
            double InitialMaximumElevationM;

            var nonZeroElevations = Field.Bins
                .Where(bin => bin.ExistingElevationM != 0.0)
                .Select(bin => bin.ExistingElevationM)
                .ToList();

            if (nonZeroElevations.Any())
            {
                InitialMinimumElevationM = nonZeroElevations.Min();
                InitialMaximumElevationM = nonZeroElevations.Max();
            }
            else
            {
                InitialMinimumElevationM = 0.0;
                InitialMaximumElevationM = 0.0;
            }

            return new double[] {  InitialMinimumElevationM, InitialMaximumElevationM };
        }

        private int[,] GenerateColorPalette()
        {
            var palette = new int[256, 3]; // 256 colors, RGB values

            for (int i = 0; i < 256; i++)
            {
                var hue = 240.0 - (i / 255.0) * 240.0; // Blue (240°) to Red (0°)
                var rgb = HsvToRgb(hue, 1.0, 1.0);

                palette[i, 0] = (int)(rgb[0] * 255); // Red
                palette[i, 1] = (int)(rgb[1] * 255); // Green
                palette[i, 2] = (int)(rgb[2] * 255); // Blue
            }

            return palette;
        }

        private double[] HsvToRgb(double h, double s, double v)
        {
            h = h % 360.0;
            if (h < 0) h += 360.0;

            var c = v * s;
            var x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
            var m = v - c;

            double r, g, b;

            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new double[] { r + m, g + m, b + m };
        }

        private double?[,] CreateElevationGrid(List<Bin> bins, int minX, int maxX, int minY, int maxY, int gridWidth, int gridHeight)
        {
            var elevationGrid = new double?[gridHeight, gridWidth];

            foreach (var bin in bins)
            {
                if (bin.ExistingElevationM == 0 && bin.X > 5)
                {
                    bin.ExistingElevationM = 0;
                }

                var x = bin.X - minX;
                var y = bin.Y - minY;

                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    elevationGrid[y, x] = bin.ExistingElevationM;
                }
            }

            return elevationGrid;
        }
    }
}
