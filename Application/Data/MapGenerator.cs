using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public class MapGenerator
    {
        private Color Background = Color.LightGray;

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
        /// <param name="ScaleFactor">Zoom level for map</param>
        /// <param name="TractorX">UTM X location of tractor</param>
        /// <param name="TractorY">UTM Y location of tractor</param>
        /// <param name="TractorHeading">Heading of tractor in degrees</param>
        /// <returns>Generated bitmap</returns>
        public Bitmap Generate
            (
            Field Field,
            int Width,
            int Height,
            bool ShowGrid,
            double ScaleFactor,
            double TractorX,
            double TractorY,
            double TractorHeading
            )
        {
            if (Field.Bins.Count == 0)
            {
                throw new InvalidOperationException("No bin data available for map generation");
            }

            // Create bitmap directly in memory
            //Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            // Calculate grid dimensions
            var bins = Field.Bins;
            var minX = bins.Min(b => b.X);
            var maxX = bins.Max(b => b.X);
            var minY = bins.Min(b => b.Y);
            var maxY = bins.Max(b => b.Y);

            var gridWidth = maxX - minX + 1;
            var gridHeight = maxY - minY + 1;

            // Calculate image dimensions
            int scaledWidth = (int)(gridWidth * ScaleFactor);
            int scaledHeight = (int)(gridHeight * ScaleFactor);

            var elevationGrid = CreateElevationGrid(bins, minX, maxX, minY, maxY, gridWidth, gridHeight);

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

            // Create bitmap directly in memory
            Bitmap bitmap = new Bitmap(scaledWidth, scaledHeight, PixelFormat.Format24bppRgb);
            
            // Lock bitmap data for direct memory access
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, scaledWidth, scaledHeight),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                int stride = bitmapData.Stride;
                int rowPadding = stride - (scaledWidth * 3);
                
                // Allocate buffer for one row of pixel data (BGR format)
                byte[] rowData = new byte[stride];

                // Write pixel data directly to bitmap memory (rows stored bottom-to-top in BMP)
                for (int y = 0; y < scaledHeight; y++)
                {
                    // BMP format stores rows bottom-to-top, so we need to flip Y
                    int bmpY = scaledHeight - 1 - y;
                    int rowOffset = 0;

                    for (int x = 0; x < scaledWidth; x++)
                    {
                        var gridX = (int)(x / ScaleFactor);
                        var gridY = (int)(y / ScaleFactor);

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
                    IntPtr rowPtr = new IntPtr(bitmapData.Scan0.ToInt64() + (bmpY * stride));
                    Marshal.Copy(rowData, 0, rowPtr, stride);
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

                int CenterX = Map.Width / 2;
                int CenterY = Map.Height / 2;

                g.DrawImage(Properties.Resources.navarrow_256px, CenterX - 24, CenterY - 24, 48, 48);
            }
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
