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
        /// Runs WhiteboxTools D-Infinity flow accumulation on a DEM (same as Python demtoflow.py).
        /// Writes the flow accumulation raster to <paramref name="outputFlowTif"/> (out_type=cells).
        /// </summary>
        /// <param name="inputDemPath">Full path to the input DEM GeoTIFF (e.g. from field.GenerateElevationDEM).</param>
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
