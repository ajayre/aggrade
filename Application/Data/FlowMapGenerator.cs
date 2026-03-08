using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BitMiracle.LibTiff.Classic;
using OpenCvSharp;

namespace AgGrade.Data
{
    /// <summary>
    /// Generates DEM (Digital Elevation Model) GeoTIFFs from field bin elevation data,
    /// matching the behavior of the Python demgenerator (FieldState -> georeferenced TIFF).
    /// </summary>
    public class FlowMapGenerator
    {
        private const string WHITEBOXTOOLS = "whitebox_tools.exe";

        /// <summary>Python helper script to write GeoTIFF via rasterio (same layout as demgenerator).</summary>
        private const string WRITE_GEOTIFF_SCRIPT = "write_geotiff_from_raw.py";

        /// <summary>No-data value for cells with no height (e.g. zero or missing).</summary>
        private const float NoDataValue = -9999f;

        /// <summary>Default Gaussian sigma for smoothing (matches Python demgenerator).</summary>
        private const double DefaultSmoothSigma = 3.0;

        /// <summary>GDAL no-data tag (for reading flow TIFFs).</summary>
        private const int GdalNoDataTag = 42113;

        /// <summary>Flow accumulation value range: 0 = white, 10 = black (matches demtoflow.py).</summary>
        private const double FlowValueMin = 0.0;
        private const double FlowValueMax = 10.0;

        private static Tiff.TiffExtendProc? s_flowTiffTagExtender;

        private static void FlowTiffTagExtender(Tiff tif)
        {
            var infos = new[]
            {
                new TiffFieldInfo((TiffTag)GdalNoDataTag, -1, -1, TiffType.ASCII, FieldBit.Custom, true, false, "GDAL_NODATA"),
            };
            tif.MergeFieldInfo(infos, infos.Length);
        }

        /// <summary>
        /// Converts a surface water flow DEM (WhiteboxTools D-Infinity output) to a PNG:
        /// 0 = white, 10 = black, no-data = white (matches demtoflow.py).
        /// </summary>
        /// <param name="FlowDEMFile">Flow DEM TIFF to convert</param>
        /// <param name="PNGFile">Path and name of PNG file to write</param>
        /// <param name="Transparent">When true, output has alpha for overlay: black=opaque, white=fully transparent, grey=proportional alpha.</param>
        /// <param name="Opacity">When Transparent is true: 100 = full transparency effect (current behavior); 0 = no transparency (image fully opaque).</param>
        public void ConvertFlowDEMtoPNG
            (
            string FlowDEMFile,
            string PNGFile,
            bool Transparent = true,
            int Opacity = 100
            )
        {
            if (string.IsNullOrWhiteSpace(FlowDEMFile))
                throw new ArgumentException("Flow DEM path is required.", nameof(FlowDEMFile));
            if (string.IsNullOrWhiteSpace(PNGFile))
                throw new ArgumentException("PNG output path is required.", nameof(PNGFile));

            string flowPath = Path.GetFullPath(FlowDEMFile);
            if (!File.Exists(flowPath))
                throw new FileNotFoundException("Flow DEM not found.", flowPath);

            if (s_flowTiffTagExtender == null)
                s_flowTiffTagExtender = Tiff.SetTagExtender(FlowTiffTagExtender);

            double? nodata = null;
            int width, height;
            double[,] data;

            using (Tiff tif = Tiff.Open(flowPath, "r"))
            {
                if (tif == null)
                    throw new InvalidOperationException($"Could not open flow TIFF: {flowPath}");

                width = tif.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                height = tif.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                int bitsPerSample = tif.GetField(TiffTag.BITSPERSAMPLE) is { } bpsField && bpsField.Length > 0
                    ? bpsField[0].ToInt() : 32;
                SampleFormat fmt = SampleFormat.IEEEFP;
                if (tif.GetField(TiffTag.SAMPLEFORMAT) is { } fmtField && fmtField.Length > 0)
                    fmt = (SampleFormat)fmtField[0].ToInt();

                if (tif.GetField((TiffTag)GdalNoDataTag) is { } nodataField && nodataField.Length > 0 &&
                    nodataField[0].ToString() is string nodataStr && double.TryParse(nodataStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double nd))
                    nodata = nd;

                int bytesPerSample = Math.Max(1, bitsPerSample / 8);
                int rowBytes = width * bytesPerSample;
                byte[] rowBuf = new byte[rowBytes];
                data = new double[height, width];

                for (int row = 0; row < height; row++)
                {
                    if (!tif.ReadScanline(rowBuf, row, 0))
                        throw new InvalidOperationException($"Failed to read scanline {row}.");
                    if (bytesPerSample == 4 && fmt == SampleFormat.IEEEFP)
                    {
                        for (int c = 0; c < width; c++)
                            data[row, c] = BitConverter.ToSingle(rowBuf, c * 4);
                    }
                    else if (bytesPerSample == 4 && (fmt == SampleFormat.UINT || fmt == SampleFormat.INT))
                    {
                        for (int c = 0; c < width; c++)
                            data[row, c] = BitConverter.ToUInt32(rowBuf, c * 4);
                    }
                    else if (bytesPerSample == 2)
                    {
                        for (int c = 0; c < width; c++)
                            data[row, c] = BitConverter.ToUInt16(rowBuf, c * 2);
                    }
                    else
                    {
                        for (int c = 0; c < width; c++)
                            data[row, c] = rowBuf[c];
                    }
                }
            }

            double span = FlowValueMax - FlowValueMin;
            byte[] grey = new byte[height * width];
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    double v = data[r, c];
                    bool valid = !nodata.HasValue || v != nodata.Value;
                    if (!valid)
                    {
                        grey[r * width + c] = 255;
                        continue;
                    }
                    double scaled = Math.Clamp(v, FlowValueMin, FlowValueMax);
                    grey[r * width + c] = (byte)(255.0 * (1.0 - (scaled - FlowValueMin) / span));
                }
            }

            if (Transparent)
            {
                // Output BGRA for overlay: black=opaque, white=transparent. Opacity 100 = full transparency effect, 0 = no transparency (all opaque).
                int opacityPct = Math.Clamp(Opacity, 0, 100);
                byte[] bgra = new byte[height * width * 4];
                for (int i = 0; i < grey.Length; i++)
                {
                    byte g = grey[i];
                    bgra[i * 4 + 0] = g;
                    bgra[i * 4 + 1] = g;
                    bgra[i * 4 + 2] = g;
                    // 100 = alpha 255-g (current); 0 = alpha 255 (no transparency)
                    bgra[i * 4 + 3] = (byte)(255 - (g * opacityPct) / 100);
                }
                using (var mat = new Mat(height, width, MatType.CV_8UC4))
                {
                    Marshal.Copy(bgra, 0, mat.Data, bgra.Length);
                    if (!Cv2.ImWrite(PNGFile, mat))
                        throw new InvalidOperationException($"Failed to write PNG: {PNGFile}");
                }
            }
            else
            {
                using (var mat = new Mat(height, width, MatType.CV_8UC1))
                {
                    Marshal.Copy(grey, 0, mat.Data, grey.Length);
                    if (!Cv2.ImWrite(PNGFile, mat))
                        throw new InvalidOperationException($"Failed to write PNG: {PNGFile}");
                }
            }
        }

        public enum ElevationTypes
        {
            Initial,
            Current,
            Target
        }
        
        /// <summary>
        /// Creates a DEM georeferenced TIFF from field bin elevation. The elevation source is
        /// determined by <paramref name="ElevationType"/> (Initial, Current, or Target). Zero elevation is treated as no-data.
        /// Writes a .tfw world file for UTM georeferencing (same convention as Python demgenerator).
        /// </summary>
        /// <param name="field">Field to convert</param>
        /// <param name="ElevationType">Type of elevation to use</param>
        /// <param name="outputFile">Path and name of TIFF to generate</param>
        /// <param name="SWCorner">On return set to SW corner in latitude and longitude</param>
        /// <param name="NECorner">On return set to NE corner in latitude and longitude</param>
        /// <param name="enableSmoothing">If true (default), apply Gaussian smoothing to soften bin edges.</param>
        public void GenerateElevationDEM
            (
            Field field,
            ElevationTypes ElevationType,
            string outputFile,
            out Coordinate SWCorner,
            out Coordinate NECorner,
            bool enableSmoothing = true
            )
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (string.IsNullOrWhiteSpace(outputFile))
                throw new ArgumentException("Output file path is required.", nameof(outputFile));
            if (field.Bins == null || field.Bins.Count == 0)
                throw new InvalidOperationException("Field has no bins.");

            int minX = field.Bins.Min(b => b.X);
            int maxX = field.Bins.Max(b => b.X);
            int minY = field.Bins.Min(b => b.Y);
            int maxY = field.Bins.Max(b => b.Y);

            int ncols = maxX - minX + 1;
            int nrows = maxY - minY + 1;

            // Build raster: row 0 = north (y = maxY), same as Python _grid_to_array
            float[,] data = new float[nrows, ncols];
            for (int r = 0; r < nrows; r++)
                for (int c = 0; c < ncols; c++)
                    data[r, c] = NoDataValue;

            foreach (Bin bin in field.Bins)
            {
                double h = ElevationType switch
                {
                    ElevationTypes.Initial => bin.InitialElevationM,
                    ElevationTypes.Current => bin.CurrentElevationM,
                    ElevationTypes.Target => bin.TargetElevationM,
                };
                bool treatAsNoData = h == 0.0;
                float value = treatAsNoData ? NoDataValue : (float)h;
                int row = maxY - bin.Y;  // row 0 = north
                int col = bin.X - minX;
                if (row >= 0 && row < nrows && col >= 0 && col < ncols)
                    data[row, col] = value;
            }

            // Gaussian smooth to soften bin edges (NoData excluded from kernel, like Python _smooth_dem_array)
            if (enableSmoothing && DefaultSmoothSigma > 0)
                data = SmoothDemArray(data, nrows, ncols, NoDataValue, DefaultSmoothSigma);

            // Top-left corner in UTM (same as Python): pixel (0,0) -> (min_x_utm, min_y_utm + nrows*BIN_SIZE_M)
            double topLeftX = field.FieldMinX + minX * Field.BIN_SIZE_M;
            double topLeftY = field.FieldMinY + (maxY + 1) * Field.BIN_SIZE_M;

            // Bounding box: SW = west/south edges, NE = east/north edges (in lat/lon)
            double swEasting = topLeftX;
            double swNorthing = topLeftY - nrows * Field.BIN_SIZE_M;
            double neEasting = topLeftX + ncols * Field.BIN_SIZE_M;
            double neNorthing = topLeftY;
            UTM.ToLatLon(field.UTMZone, field.IsNorthernHemisphere, swEasting, swNorthing, out double swLat, out double swLon);
            UTM.ToLatLon(field.UTMZone, field.IsNorthernHemisphere, neEasting, neNorthing, out double neLat, out double neLon);
            SWCorner = new Coordinate(swLat, swLon);
            NECorner = new Coordinate(neLat, neLon);

            WriteGeoTiff(data, nrows, ncols, outputFile, topLeftX, topLeftY, field.UTMZone, field.IsNorthernHemisphere);
            WriteWorldFile(outputFile, topLeftX, topLeftY);
        }

        private static void WriteGeoTiff(float[,] data, int nrows, int ncols, string outputPath,
            double topLeftEasting, double topLeftNorthing, int utmZone, bool isNorthernHemisphere)
        {
            string scriptPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location) + Path.DirectorySeparatorChar + WRITE_GEOTIFF_SCRIPT;
            string rawPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".bin");
            string metaPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
            try
            {
                // Write raw float32 row-major
                using (var fs = File.Create(rawPath))
                using (var bw = new BinaryWriter(fs))
                {
                    for (int r = 0; r < nrows; r++)
                        for (int c = 0; c < ncols; c++)
                            bw.Write(data[r, c]);
                }

                // Write metadata JSON (invariant culture for numbers)
                var inv = CultureInfo.InvariantCulture;
                string json = "{" +
                    "\"nrows\":" + nrows + "," +
                    "\"ncols\":" + ncols + "," +
                    "\"topLeftEasting\":" + topLeftEasting.ToString("R", inv) + "," +
                    "\"topLeftNorthing\":" + topLeftNorthing.ToString("R", inv) + "," +
                    "\"utmZone\":" + utmZone + "," +
                    "\"isNorthernHemisphere\":" + (isNorthernHemisphere ? "true" : "false") + "," +
                    "\"nodata\":" + NoDataValue.ToString("R", inv) + "}";
                File.WriteAllText(metaPath, json);

                string outputFull = Path.GetFullPath(outputPath);
                var startInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"\"{scriptPath}\" \"{rawPath}\" \"{metaPath}\" \"{outputFull}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? ".",
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Failed to start Python for GeoTIFF write.");
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        string msg = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                        throw new InvalidOperationException(
                            $"GeoTIFF write failed (Python exit {process.ExitCode}). {msg}");
                    }
                }
            }
            finally
            {
                try { if (File.Exists(rawPath)) File.Delete(rawPath); } catch { }
                try { if (File.Exists(metaPath)) File.Delete(metaPath); } catch { }
            }
        }

        /// <summary>
        /// Gaussian smooth the DEM so bin edges are softened. NoData pixels are excluded
        /// from the kernel (weighted average over valid neighbors only), matching Python _smooth_dem_array.
        /// Uses same truncate (4.0) and separable 1D Gaussian as scipy.ndimage.gaussian_filter.
        /// </summary>
        private static float[,] SmoothDemArray(float[,] data, int nrows, int ncols, float nodata, double sigma)
        {
            if (sigma <= 0) return data;

            // Match scipy.ndimage.gaussian_filter: truncate=4.0 -> radius = ceil(4*sigma), kernel size 2*radius+1
            int radius = (int)Math.Ceiling(4.0 * sigma);
            int kernelSize = 2 * radius + 1;
            double[] kernel1D = BuildGaussianKernel1D(sigma, kernelSize);

            double[,] v = new double[nrows, ncols];
            double[,] w = new double[nrows, ncols];
            for (int r = 0; r < nrows; r++)
            {
                for (int c = 0; c < ncols; c++)
                {
                    bool valid = data[r, c] != nodata;
                    v[r, c] = valid ? data[r, c] : 0.0;
                    w[r, c] = valid ? 1.0 : 0.0;
                }
            }

            // Separable: smooth rows then columns (same as scipy)
            Convolve1DRows(v, w, nrows, ncols, kernel1D, kernelSize, radius);
            Convolve1DCols(v, w, nrows, ncols, kernel1D, kernelSize, radius);

            float[,] result = new float[nrows, ncols];
            for (int r = 0; r < nrows; r++)
            {
                for (int c = 0; c < ncols; c++)
                {
                    if (data[r, c] == nodata)
                    {
                        result[r, c] = nodata;
                        continue;
                    }
                    double wVal = Math.Max(w[r, c], 1e-12);
                    result[r, c] = (float)(v[r, c] / wVal);
                }
            }
            return result;
        }

        private static double[] BuildGaussianKernel1D(double sigma, int size)
        {
            int half = size / 2;
            double[] k = new double[size];
            double sum = 0;
            for (int i = 0; i < size; i++)
            {
                double x = i - half;
                double g = Math.Exp(-(x * x) / (2 * sigma * sigma));
                k[i] = g;
                sum += g;
            }
            for (int i = 0; i < size; i++)
                k[i] /= sum;
            return k;
        }

        private static void Convolve1DRows(double[,] v, double[,] w, int nrows, int ncols, double[] kernel, int kSize, int kHalf)
        {
            double[] rowV = new double[ncols];
            double[] rowW = new double[ncols];
            for (int r = 0; r < nrows; r++)
            {
                for (int c = 0; c < ncols; c++)
                {
                    double sumV = 0, sumW = 0;
                    for (int k = 0; k < kSize; k++)
                    {
                        int sc = Math.Clamp(c + k - kHalf, 0, ncols - 1);
                        sumV += v[r, sc] * kernel[k];
                        sumW += w[r, sc] * kernel[k];
                    }
                    rowV[c] = sumV;
                    rowW[c] = sumW;
                }
                for (int c = 0; c < ncols; c++)
                {
                    v[r, c] = rowV[c];
                    w[r, c] = rowW[c];
                }
            }
        }

        private static void Convolve1DCols(double[,] v, double[,] w, int nrows, int ncols, double[] kernel, int kSize, int kHalf)
        {
            double[] colV = new double[nrows];
            double[] colW = new double[nrows];
            for (int c = 0; c < ncols; c++)
            {
                for (int r = 0; r < nrows; r++)
                {
                    double sumV = 0, sumW = 0;
                    for (int k = 0; k < kSize; k++)
                    {
                        int sr = Math.Clamp(r + k - kHalf, 0, nrows - 1);
                        sumV += v[sr, c] * kernel[k];
                        sumW += w[sr, c] * kernel[k];
                    }
                    colV[r] = sumV;
                    colW[r] = sumW;
                }
                for (int r = 0; r < nrows; r++)
                {
                    v[r, c] = colV[r];
                    w[r, c] = colW[r];
                }
            }
        }

        /// <summary>
        /// Writes a .tfw world file so the TIFF is georeferenced in UTM.
        /// topLeftX/topLeftY are the raster top-left corner; .tfw uses center of upper-left pixel.
        /// </summary>
        private static void WriteWorldFile(string tiffPath, double topLeftX, double topLeftY)
        {
            string dir = Path.GetDirectoryName(tiffPath) ?? ".";
            string baseName = Path.GetFileNameWithoutExtension(tiffPath);
            string tfwPath = Path.Combine(dir, baseName + ".tfw");

            double halfCell = Field.BIN_SIZE_M * 0.5;
            double centerX = topLeftX + halfCell;
            double centerY = topLeftY - halfCell;

            var inv = CultureInfo.InvariantCulture;
            using (var writer = new StreamWriter(tfwPath, false))
            {
                writer.WriteLine(Field.BIN_SIZE_M.ToString("R", inv));
                writer.WriteLine("0");
                writer.WriteLine("0");
                writer.WriteLine((-Field.BIN_SIZE_M).ToString("R", inv));
                writer.WriteLine(centerX.ToString("R", inv));
                writer.WriteLine(centerY.ToString("R", inv));
            }
        }

        /// <summary>
        /// Runs WhiteboxTools D-Infinity flow accumulation on a DEM (same as Python demtoflow.py).
        /// Writes the flow accumulation raster to <paramref name="outputFlowTif"/> (out_type=cells).
        /// </summary>
        /// <param name="inputDemPath">Full path to the input DEM GeoTIFF (e.g. from GenerateElevationDEM).</param>
        /// <param name="outputFlowTif">Full path for the output flow accumulation TIFF.</param>
        public void GenerateFlowDEM
            (
            string inputDemPath,
            string outputFlowTif
            )
        {
            if (string.IsNullOrWhiteSpace(inputDemPath))
                throw new ArgumentException("Input DEM path is required.", nameof(inputDemPath));
            if (string.IsNullOrWhiteSpace(outputFlowTif))
                throw new ArgumentException("Output flow TIFF path is required.", nameof(outputFlowTif));

            string inputFull = Path.GetFullPath(inputDemPath);
            if (!File.Exists(inputFull))
                throw new FileNotFoundException("Input DEM not found.", inputFull);

            string CurrentFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)! + Path.DirectorySeparatorChar;
            string wbExe = CurrentFolder + WHITEBOXTOOLS;

            if (!File.Exists(wbExe))
                throw new FileNotFoundException($"WhiteboxTools not found: {wbExe}", wbExe);

            string workingDir = Directory.GetCurrentDirectory();
            string outputFull = Path.GetFullPath(outputFlowTif);

            var startInfo = new ProcessStartInfo
            {
                FileName = wbExe,
                Arguments = string.Join(" ",
                    "-r=DInfFlowAccumulation",
                    "-v",
                    "--log",
                    $"--wd=\"{workingDir}\"",
                    $"--input=\"{inputFull}\"",
                    "--out_type=cells",
                    $"-o=\"{outputFull}\""),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDir,
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Failed to start WhiteboxTools.");

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string message = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                    throw new InvalidOperationException(
                        $"WhiteboxTools D-Infinity flow accumulation failed (exit code {process.ExitCode}). {message}");
                }
            }
        }
    }
}
