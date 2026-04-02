using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public class Survey
    {
        private string FileName;

        /// <summary>
        /// Only Latitude, Longitude and ExistingElevation are used
        /// </summary>
        public List<TopologyPoint> InteriorPoints = new List<TopologyPoint>();

        /// <summary>
        /// Only Latitude, Longitude and ExistingElevation are used
        /// </summary>
        public List<TopologyPoint> BoundaryPoints = new List<TopologyPoint>();

        public List<Benchmark> Benchmarks = new List<Benchmark>();

        public string Name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(FileName);
            }
        }

        /// <summary>
        /// Adds a new benchmark to the survey.
        /// If this is the first benchmark it is named MB; otherwise it is named
        /// BMx where x is the next available sequence number starting at 1.
        /// </summary>
        /// <param name="Location">Benchmark location as latitude/longitude.</param>
        /// <param name="ElevationM">Benchmark elevation in meters.</param>
        /// <returns>The newly created benchmark instance.</returns>
        public Benchmark AddBenchmark
            (
            Coordinate Location,
            double ElevationM
            )
        {
            if (Location == null)
                throw new ArgumentNullException(nameof(Location));

            string name;
            if (Benchmarks.Count == 0)
            {
                name = "MB";
            }
            else
            {
                int maxSequence = 0;
                foreach (Benchmark existing in Benchmarks)
                {
                    if (existing == null || string.IsNullOrWhiteSpace(existing.Name)) continue;

                    Match match = Regex.Match(existing.Name.Trim(), @"^BM(\d+)$", RegexOptions.IgnoreCase);
                    if (!match.Success) continue;

                    if (int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sequence))
                    {
                        if (sequence > maxSequence) maxSequence = sequence;
                    }
                }

                name = $"BM{maxSequence + 1}";
            }

            Benchmark benchmark = new Benchmark(Location, name, ElevationM);
            Benchmarks.Add(benchmark);
            return benchmark;
        }

        /// <summary>
        /// Loads a trimble multiplane file
        /// </summary>
        /// <param name="FileName">Path and name of the file to load</param>
        public void LoadFromMultiplane
            (
            string FileName
            )
        {
            if (string.IsNullOrWhiteSpace(FileName))
                throw new ArgumentException("File name is required.", nameof(FileName));

            if (!File.Exists(FileName))
                throw new FileNotFoundException("Multiplane file was not found.", FileName);

            this.FileName = FileName;

            string[] Lines = File.ReadAllLines(FileName);
            InteriorPoints.Clear();
            BoundaryPoints.Clear();
            Benchmarks.Clear();

            // Support an empty/blank file as a valid new survey.
            if (Lines.Length == 0 || Lines.All(string.IsNullOrWhiteSpace))
                return;

            string[] HeaderParts = Lines[0].Split('\t');
            if (HeaderParts.Length < 5)
                throw new Exception("Invalid multiplane header. Expected at least 5 tab-separated columns.");

            double BaseOffsetX = ParseDouble(HeaderParts[1], "header offset X");
            double BaseOffsetY = ParseDouble(HeaderParts[2], "header offset Y");
            double BaseHeightFt = ParseDouble(HeaderParts[3], "header elevation");

            Coordinate BaseMasterLocation = ParseHeaderLocation(HeaderParts[4]);
            Coordinate MasterLocation = UTM.OffsetLocation(BaseMasterLocation, BaseOffsetX, BaseOffsetY);

            // Multiplane elevation values are interpreted directly as feet relative
            // to the file's internal datum, then converted to meters for storage.
            double masterElevationMeters = FeetToMeters(BaseHeightFt);

            Benchmarks.Add(new Benchmark(MasterLocation, "MB", masterElevationMeters));

            for (int li = 1; li < Lines.Length; li++)
            {
                string line = Lines[li];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split('\t');
                if (parts.Length < 4)
                    throw new Exception($"Unable to parse line {li + 1}. Expected at least 4 tab-separated columns.");

                double x = ParseDouble(parts[1], $"line {li + 1} offset X");
                double y = ParseDouble(parts[2], $"line {li + 1} offset Y");
                double elevationFt = ParseDouble(parts[3], $"line {li + 1} elevation");

                string code = parts.Length > 4 ? parts[4].Trim() : string.Empty;
                double elevationM = FeetToMeters(elevationFt);
                Coordinate pointLocation = UTM.OffsetLocation(MasterLocation, x, y);

                if (IsBenchmarkCode(code))
                {
                    Benchmarks.Add(new Benchmark(pointLocation, code, elevationM));
                    continue;
                }

                TopologyPoint point = new TopologyPoint
                {
                    Latitude = pointLocation.Latitude,
                    Longitude = pointLocation.Longitude,
                    ExistingElevation = elevationM
                };

                if (string.Equals(code, "B", StringComparison.OrdinalIgnoreCase))
                    BoundaryPoints.Add(point);
                else
                    InteriorPoints.Add(point);
            }
        }

        /// <summary>
        /// Normalizes survey elevations for multiplane export by forcing the
        /// master benchmark to 30.48 m (100 ft) and applying the same offset
        /// to every other stored elevation so relative differences are preserved.
        /// </summary>
        public void NormalizeElevationsForMultiplane
            (
            )
        {
            const double MultiplaneMasterElevationM = 30.48;

            Benchmark masterBenchmark = Benchmarks.FirstOrDefault(b =>
                b != null &&
                !string.IsNullOrWhiteSpace(b.Name) &&
                string.Equals(b.Name.Trim(), "MB", StringComparison.OrdinalIgnoreCase))
                ?? throw new Exception("Cannot normalize elevations: no master benchmark (MB) exists.");

            double deltaM = MultiplaneMasterElevationM - masterBenchmark.Elevation;
            if (deltaM == 0) return;

            foreach (Benchmark benchmark in Benchmarks)
            {
                if (benchmark == null) continue;
                benchmark.Elevation += deltaM;
            }

            foreach (TopologyPoint point in BoundaryPoints)
            {
                if (point == null) continue;
                point.ExistingElevation += deltaM;
            }

            foreach (TopologyPoint point in InteriorPoints)
            {
                if (point == null) continue;
                point.ExistingElevation += deltaM;
            }
        }

        /// <summary>
        /// Saves the survey to a trimble multiplane file
        /// If the file exists then it is overwritten without warning
        /// </summary>
        /// <param name="FileName">Path and name of the file to save to</param>
        public void SaveToMultiplane
            (
            string FileName
            )
        {
            if (string.IsNullOrWhiteSpace(FileName))
                throw new ArgumentException("File name is required.", nameof(FileName));

            NormalizeElevationsForMultiplane();

            Benchmark masterBenchmark = Benchmarks.FirstOrDefault(b =>
                b != null &&
                !string.IsNullOrWhiteSpace(b.Name) &&
                string.Equals(b.Name.Trim(), "MB", StringComparison.OrdinalIgnoreCase))
                ?? throw new Exception("Cannot save multiplane file: no master benchmark (MB) exists.");

            Coordinate masterLocation = masterBenchmark.Location
                ?? throw new Exception("Cannot save multiplane file: master benchmark location is null.");

            double masterHeightFt = MetersToFeet(masterBenchmark.Elevation);

            List<string> lines = new List<string>();
            lines.Add(string.Format(
                CultureInfo.InvariantCulture,
                "0001\t0.000\t0.000\t{0:0.000}\tMB {1} / {2}\t0.000",
                masterHeightFt,
                FormatLatitude(masterLocation.Latitude),
                FormatLongitude(masterLocation.Longitude)));

            UTM.UTMCoordinate masterUtm = UTM.FromLatLon(masterLocation.Latitude, masterLocation.Longitude);
            int nextId = 2;

            foreach (Benchmark benchmark in Benchmarks)
            {
                if (benchmark == null) continue;
                if (string.Equals(benchmark.Name?.Trim(), "MB", StringComparison.OrdinalIgnoreCase)) continue;

                lines.Add(FormatPointLine(nextId++, masterUtm, benchmark.Location, benchmark.Elevation, benchmark.Name));
            }

            foreach (TopologyPoint point in BoundaryPoints)
            {
                if (point == null) continue;
                lines.Add(FormatPointLine(nextId++, masterUtm, new Coordinate(point.Latitude, point.Longitude), point.ExistingElevation, "B"));
            }

            foreach (TopologyPoint point in InteriorPoints)
            {
                if (point == null) continue;
                lines.Add(FormatPointLine(nextId++, masterUtm, new Coordinate(point.Latitude, point.Longitude), point.ExistingElevation, string.Empty));
            }

            File.WriteAllLines(FileName, lines);
        }

        /// <summary>
        /// Formats a non-header multiplane point row from a geodetic location and relative elevation.
        /// </summary>
        /// <param name="PointId">Sequential point identifier to write in column 1.</param>
        /// <param name="MasterUtm">UTM coordinate of the master benchmark used as the offset origin.</param>
        /// <param name="Location">Point latitude/longitude location.</param>
        /// <param name="ElevationM">Point elevation in meters relative to the master normalization.</param>
        /// <param name="Code">Point code such as BM1, B, or other survey code.</param>
        /// <returns>One tab-delimited multiplane point line.</returns>
        private static string FormatPointLine
            (
            int PointId,
            UTM.UTMCoordinate MasterUtm,
            Coordinate Location,
            double ElevationM,
            string Code
            )
        {
            if (Location == null)
                throw new Exception($"Point {PointId} has a null location.");

            UTM.UTMCoordinate utm = UTM.FromLatLon(Location.Latitude, Location.Longitude);
            if (utm.Zone != MasterUtm.Zone || utm.IsNorthernHemisphere != MasterUtm.IsNorthernHemisphere)
                throw new Exception($"Point {PointId} is not in the same UTM zone/hemisphere as the master benchmark.");

            double relativeX = utm.Easting - MasterUtm.Easting;
            double relativeY = utm.Northing - MasterUtm.Northing;

            double elevationFt = MetersToFeet(ElevationM);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}\t{1:0.00}\t{2:0.00}\t{3:0.000000}\t{4}",
                PointId,
                relativeX,
                relativeY,
                elevationFt,
                string.IsNullOrWhiteSpace(Code) ? string.Empty : Code.Trim());
        }

        /// <summary>
        /// Parses the header location segment in the format "MB N.. / W..".
        /// </summary>
        /// <param name="HeaderLocationText">Raw header location text from column 5.</param>
        /// <returns>Parsed master benchmark latitude/longitude.</returns>
        private static Coordinate ParseHeaderLocation
            (
            string HeaderLocationText
            )
        {
            if (string.IsNullOrWhiteSpace(HeaderLocationText))
                throw new Exception("Invalid multiplane header location: value is empty.");

            int slashIndex = HeaderLocationText.IndexOf('/');
            if (slashIndex < 0)
                throw new Exception($"Invalid multiplane header location: '{HeaderLocationText}'.");

            string left = HeaderLocationText.Substring(0, slashIndex).Trim();
            string right = HeaderLocationText.Substring(slashIndex + 1).Trim();

            if (!left.StartsWith("MB ", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Invalid multiplane header location prefix: '{HeaderLocationText}'.");

            string latText = left.Substring(3).Trim();
            double latitude = ParseDms(latText, 'N', 'S', "latitude");
            double longitude = ParseDms(right, 'E', 'W', "longitude");

            return new Coordinate(latitude, longitude);
        }

        /// <summary>
        /// Determines whether a code should be treated as a benchmark code.
        /// </summary>
        /// <param name="Code">Point code text to inspect.</param>
        /// <returns>True when the code represents MB or BM*.</returns>
        private static bool IsBenchmarkCode
            (
            string Code
            )
        {
            if (string.IsNullOrWhiteSpace(Code)) return false;
            string normalized = Code.Trim().ToUpperInvariant();
            return normalized == "MB" || normalized.StartsWith("BM");
        }

        /// <summary>
        /// Parses a single DMS value with hemisphere prefix (for example N36:26:51.704).
        /// </summary>
        /// <param name="Text">Text containing hemisphere and D:M:S components.</param>
        /// <param name="PositiveHemisphere">Hemisphere letter treated as positive (for example N or E).</param>
        /// <param name="NegativeHemisphere">Hemisphere letter treated as negative (for example S or W).</param>
        /// <param name="ValueName">Human-readable value name used in exception messages.</param>
        /// <returns>Signed decimal-degree value.</returns>
        private static double ParseDms
            (
            string Text,
            char PositiveHemisphere,
            char NegativeHemisphere,
            string ValueName
            )
        {
            if (string.IsNullOrWhiteSpace(Text))
                throw new Exception($"Invalid {ValueName}: value is empty.");

            string trimmed = Text.Trim();
            char hemisphere = char.ToUpperInvariant(trimmed[0]);

            if (hemisphere != char.ToUpperInvariant(PositiveHemisphere) &&
                hemisphere != char.ToUpperInvariant(NegativeHemisphere))
            {
                throw new Exception($"Invalid {ValueName} hemisphere in '{Text}'.");
            }

            string dms = trimmed.Substring(1);
            string[] parts = dms.Split(':');
            if (parts.Length != 3)
                throw new Exception($"Invalid {ValueName} format '{Text}'. Expected D:M:S.");

            double degrees = ParseDouble(parts[0], $"{ValueName} degrees");
            double minutes = ParseDouble(parts[1], $"{ValueName} minutes");
            double seconds = ParseDouble(parts[2], $"{ValueName} seconds");

            if (minutes < 0 || minutes >= 60 || seconds < 0 || seconds >= 60)
                throw new Exception($"Invalid {ValueName} DMS range in '{Text}'.");

            double value = degrees + (minutes / 60.0) + (seconds / 3600.0);
            if (hemisphere == char.ToUpperInvariant(NegativeHemisphere))
                value = -value;

            return value;
        }

        /// <summary>
        /// Formats latitude in multiplane DMS notation.
        /// </summary>
        /// <param name="Latitude">Latitude in decimal degrees.</param>
        /// <returns>Latitude formatted like N36:26:51.704.</returns>
        private static string FormatLatitude
            (
            double Latitude
            )
        {
            return FormatDms(Latitude, 'N', 'S');
        }

        /// <summary>
        /// Formats longitude in multiplane DMS notation.
        /// </summary>
        /// <param name="Longitude">Longitude in decimal degrees.</param>
        /// <returns>Longitude formatted like W090:43:42.432.</returns>
        private static string FormatLongitude
            (
            double Longitude
            )
        {
            return FormatDms(Longitude, 'E', 'W');
        }

        /// <summary>
        /// Converts a signed decimal-degree value to DMS text with hemisphere.
        /// </summary>
        /// <param name="Value">Signed decimal-degree value.</param>
        /// <param name="PositiveHemisphere">Hemisphere letter to use for non-negative values.</param>
        /// <param name="NegativeHemisphere">Hemisphere letter to use for negative values.</param>
        /// <returns>DMS-formatted coordinate text.</returns>
        private static string FormatDms
            (
            double Value,
            char PositiveHemisphere,
            char NegativeHemisphere
            )
        {
            char hemisphere = Value >= 0 ? PositiveHemisphere : NegativeHemisphere;
            double abs = Math.Abs(Value);
            int degrees = (int)Math.Floor(abs);
            double remainingMinutes = (abs - degrees) * 60.0;
            int minutes = (int)Math.Floor(remainingMinutes);
            double seconds = (remainingMinutes - minutes) * 60.0;

            // Correct rounding carry-over.
            if (seconds >= 59.9995)
            {
                seconds = 0;
                minutes += 1;
            }

            if (minutes >= 60)
            {
                minutes = 0;
                degrees += 1;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}{1:00}:{2:00}:{3:00.000}", hemisphere, degrees, minutes, seconds);
        }

        /// <summary>
        /// Parses a floating-point value using invariant culture.
        /// </summary>
        /// <param name="Text">Raw text containing a numeric value.</param>
        /// <param name="ValueName">Human-readable field name used in exception messages.</param>
        /// <returns>Parsed double value.</returns>
        private static double ParseDouble
            (
            string Text,
            string ValueName
            )
        {
            if (!double.TryParse(Text?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                throw new Exception($"Invalid numeric value for {ValueName}: '{Text}'.");
            return value;
        }

        /// <summary>
        /// Converts feet to meters.
        /// </summary>
        /// <param name="Feet">Distance in feet.</param>
        /// <returns>Distance in meters.</returns>
        private static double FeetToMeters
            (
            double Feet
            )
        {
            return Feet * 0.3048;
        }

        /// <summary>
        /// Converts meters to feet.
        /// </summary>
        /// <param name="Meters">Distance in meters.</param>
        /// <returns>Distance in feet.</returns>
        private static double MetersToFeet
            (
            double Meters
            )
        {
            return Meters / 0.3048;
        }
    }
}
