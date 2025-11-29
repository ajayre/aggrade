using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace AgGrade.Data
{
    /// <summary>
    /// Loads an AGD file
    /// Does not support fields that cross north/south hemispheres
    /// Does not support fields that cross a UTM zone
    /// </summary>
	public class AGDLoader
	{
        private const double CUBIC_YARDS_PER_CUBIC_METER = 1.30795061931439;
        //private const double ACCURACY_TOLERANCE_M = 0.03048; // 0.1ft in meters
        private const double ACCURACY_TOLERANCE_M = 0;

        public AGDLoader
			(
			)
		{
		}

        /// <summary>
        /// Loads in an AGD (Optisurface) file
        /// </summary>
        /// <param name="FileName">Path and name of file to load</param>
        /// <returns>New field</returns>
        public Field Load
			(
			string FileName
			)
		{
            Field NewField = new Field();

			LoadTopologyData(FileName, NewField);

            CalculateSiteCentroid(NewField);
            
			ConvertToLocalCoordinates(NewField);
			
			// create 2ft x 2ft bins
            CalculateCutFillVolumesWithBinning(NewField);

            FillEmptyBins(NewField);

            return NewField;
        }

        /// <summary>
        /// Locates bins that have no existing elevation data because they had
        /// no topology points placed into them, and extrapolates from
        /// a minimum of four surrounding bins that have both existing and target elevation data.
        /// Edge bins are automatically excluded as they will have fewer than four neighbors with data.
        /// </summary>
        /// <param name="Field">Field to update</param>
        private void FillEmptyBins
            (
            Field Field
            )
        {
            if (Field.Bins == null || Field.Bins.Count == 0)
                return;

            // Create a dictionary for quick bin lookup by (X, Y) coordinates
            Dictionary<(int X, int Y), Bin> binMap = new Dictionary<(int X, int Y), Bin>();
            foreach (Bin bin in Field.Bins)
            {
                binMap[(bin.X, bin.Y)] = bin;
            }

            // Process each bin
            foreach (Bin bin in Field.Bins)
            {
                // Check if bin is empty (no existing elevation data)
                // A bin is considered empty if it has no existing elevation data
                // We check if ExistingElevationM is 0, which is the default when no data was assigned
                bool isEmpty = bin.ExistingElevationM == 0 && bin.TargetElevationM == 0;

                // Skip if not empty
                if (!isEmpty)
                    continue;

                // Find surrounding bins with data (8 neighbors: N, S, E, W, NE, NW, SE, SW)
                // Only consider neighbors that have BOTH existing and target elevation data
                List<Bin> neighborsWithData = new List<Bin>();
                
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        // Skip the current bin itself
                        if (dx == 0 && dy == 0)
                            continue;

                        int neighborX = bin.X + dx;
                        int neighborY = bin.Y + dy;

                        // Check if neighbor exists and has both existing and target elevation data
                        if (binMap.TryGetValue((neighborX, neighborY), out Bin neighbor))
                        {
                            // Neighbor must have both existing and target elevation data
                            if (neighbor.ExistingElevationM != 0 && neighbor.TargetElevationM != 0)
                            {
                                neighborsWithData.Add(neighbor);
                            }
                        }
                    }
                }

                // Need at least 4 neighbors with data to fill the empty bin
                // This automatically excludes edge bins which will have fewer neighbors
                if (neighborsWithData.Count >= 4)
                {
                    // Calculate averages from all neighbors that have data
                    // Use all available neighbors, not just those with non-zero values
                    double avgExistingElevation = neighborsWithData
                        .Select(n => n.ExistingElevationM)
                        .Average();

                    double avgTargetElevation = neighborsWithData
                        .Select(n => n.TargetElevationM)
                        .Average();

                    double avgCutAmount = neighborsWithData
                        .Select(n => n.CutAmountM)
                        .Average();

                    double avgFillAmount = neighborsWithData
                        .Select(n => n.FillAmountM)
                        .Average();

                    // Fill the empty bin with averaged values
                    bin.ExistingElevationM = avgExistingElevation;
                    bin.TargetElevationM = avgTargetElevation;
                    bin.CutAmountM = avgCutAmount;
                    bin.FillAmountM = avgFillAmount;
                }
            }
        }

        /// <summary>
        /// Gets the lat,lon for the center of the field
        /// </summary>
        /// <param name="Field">Field to update</param>
        private void CalculateSiteCentroid
			(
            Field Field
			)
		{
            double TotalLat = 0;
            double TotalLon = 0;
            int Count = 0;

            foreach (TopologyPoint Point in Field.TopologyPoints)
            {
                if (Point.ExistingElevation != 0 || Point.ProposedElevation != 0)
                {
                    TotalLat += Point.Latitude;
                    TotalLon += Point.Longitude;
                    Count++;
                }
            }

            Field.FieldCentroidLat = TotalLat / Count;
            Field.FieldCentroidLon = TotalLon / Count;
        }

        /// <summary>
        /// Gets the X,Y for SW corner of field
        /// Gets the X,Y for each point in the topology
        /// </summary>
        /// <param name="Field">Field to update</param>
        private void ConvertToLocalCoordinates
            (
            Field Field
            )
        {
            // find the Southwest corner (minimum X and Y) to use as origin
            double MinLat = Field.TopologyPoints.Min(p => p.Latitude);
            double MinLon = Field.TopologyPoints.Min(p => p.Longitude);

			UTM.UTMCoordinate MinXY = UTM.FromLatLon(MinLat, MinLon);
			Field.FieldMinX = MinXY.Easting;
			Field.FieldMinY = MinXY.Northing;

            Field.UTMZone = MinXY.Zone;
            Field.IsNorthernHemisphere = MinXY.IsNorthernHemisphere;

            foreach (TopologyPoint Point in Field.TopologyPoints)
            {
				UTM.UTMCoordinate Coord = UTM.FromLatLon(Point.Latitude, Point.Longitude);
				Point.X = Coord.Easting;
				Point.Y = Coord.Northing;
            }
        }

        /// <summary>
        /// Divides the field up into 2ft x 2ft bins and calculates the cut and fill volume for each bin
        /// </summary>
        /// <param name="Field">Field to update</param>
        private void CalculateCutFillVolumesWithBinning
            (
            Field Field
            )
        {
            // Create 2ft x 2ft bins (2ft = 0.6096m)
            double BinSizeM = 0.6096;

            // Find bounds of the field
            double MinX = Field.TopologyPoints.Min(p => p.X);
            double MaxX = Field.TopologyPoints.Max(p => p.X);
            double MinY = Field.TopologyPoints.Min(p => p.Y);
            double MaxY = Field.TopologyPoints.Max(p => p.Y);

            // get size of field in bins
            int BinWidth = (int)Math.Ceiling((MaxX - MinX) / BinSizeM);
            int BinHeight = (int)Math.Ceiling((MaxY - MinY) / BinSizeM);

            // Initialize bins
            Dictionary<Point, List<double>> BinCutFillValues = new Dictionary<Point, List<double>>();
            Dictionary<Point, double> CutFillMap = new Dictionary<Point, double>();
            Dictionary<Point, List<double>> BinExistingElevationValues = new Dictionary<Point, List<double>>();
            Dictionary<Point, List<double>> BinTargetElevationValues = new Dictionary<Point, List<double>>();
            Field.Bins.Clear();

            int ProcessedPoints = 0;
            int TotalPoints = Field.TopologyPoints.Count;

            // Bin the AGD data points
            foreach (TopologyPoint Point in Field.TopologyPoints)
            {
                ProcessedPoints++;

                if (Math.Abs(Point.CutFill) > ACCURACY_TOLERANCE_M)
                {
                    // Calculate which bin this point belongs to
                    int BinX = (int)Math.Floor((Point.X - MinX) / BinSizeM);
                    int BinY = (int)Math.Floor((Point.Y - MinY) / BinSizeM);

                    // Ensure bin is within bounds
                    if (BinX >= 0 && BinX < BinWidth && BinY >= 0 && BinY < BinHeight)
                    {
                        Point binKey = new Point(BinX, BinY);
                        if (!BinCutFillValues.ContainsKey(binKey))
                        {
                            BinCutFillValues[binKey] = new List<double>();
                        }
                        BinCutFillValues[binKey].Add(Point.CutFill);

                        if (!BinExistingElevationValues.ContainsKey(binKey))
                        {
                            BinExistingElevationValues[binKey] = new List<double>();
                        }
                        BinExistingElevationValues[binKey].Add(Point.ExistingElevation);
                        
                        if (!BinTargetElevationValues.ContainsKey(binKey))
                        {
                            BinTargetElevationValues[binKey] = new List<double>();
                        }
                        BinTargetElevationValues[binKey].Add(Point.ProposedElevation);
                    }
                }
            }

            // loop through bins
            for (int y = 0; y < BinHeight; y++)
            {
                for (int x = 0; x < BinWidth; x++)
                {
                    Bin NewBin = new Bin();
                    NewBin.X = x;
                    NewBin.Y = y;

                    Point Key = new Point(x, y);

                    if (BinCutFillValues.ContainsKey(Key))
                    {
                        double AverageCutFill = BinCutFillValues[Key].Average();
                        if (AverageCutFill > 0)
                        {
                            NewBin.CutAmountM = AverageCutFill;
                            NewBin.FillAmountM = 0;
                        }
                        else
                        {
                            NewBin.CutAmountM = 0;
                            NewBin.FillAmountM = Math.Abs(AverageCutFill);
                        }
                    }

                    if (BinExistingElevationValues.ContainsKey(Key))
                    {
                        NewBin.ExistingElevationM = BinExistingElevationValues[Key].Average();
                    }
                    if (BinTargetElevationValues.ContainsKey(Key))
                    {
                        NewBin.TargetElevationM = BinTargetElevationValues[Key].Average();
                    }

                    // get bin coords and centroid
                    double BinMinX = MinX + NewBin.X * BinSizeM;
                    double BinMaxX = MinX + (NewBin.X + 1) * BinSizeM;
                    double BinMinY = MinY + NewBin.Y * BinSizeM;
                    double BinMaxY = MinY + (NewBin.Y + 1) * BinSizeM;

                    // Calculate centroid coordinates
                    double CentroidX = (BinMinX + BinMaxX) / 2.0;
                    double CentroidY = (BinMinY + BinMaxY) / 2.0;

                    double Lat;
                    double Lon;
                    UTM.ToLatLon(Field.UTMZone, Field.IsNorthernHemisphere, BinMinX, BinMinY, out Lat, out Lon);
                    NewBin.SouthwestCorner = new Coordinate() { Latitude = Lat, Longitude = Lon };

                    UTM.ToLatLon(Field.UTMZone, Field.IsNorthernHemisphere, BinMaxX, BinMaxY, out Lat, out Lon);
                    NewBin.NortheastCorner = new Coordinate() { Latitude = Lat, Longitude = Lon };

                    UTM.ToLatLon(Field.UTMZone, Field.IsNorthernHemisphere, CentroidX, CentroidY, out Lat, out Lon);
                    NewBin.Centroid = new Coordinate() { Latitude = Lat, Longitude = Lon };

                    Field.Bins.Add(NewBin);
                }
            }

            // Calculate volumes using proper 2ft x 2ft bin areas
            double TotalCutVolumeM3 = 0;
            double TotalFillVolumeM3 = 0;
            double BinAreaM2 = BinSizeM * BinSizeM; // 0.3716 m� per bin (4 ft�)

            foreach (Bin b in Field.Bins)
            {
                TotalCutVolumeM3 += (b.CutAmountM * BinAreaM2);
                TotalFillVolumeM3 += (b.FillAmountM * BinAreaM2);
            }

            Field.TotalCutBCY = TotalCutVolumeM3 * CUBIC_YARDS_PER_CUBIC_METER;
            Field.TotalFillBCY = TotalFillVolumeM3 * CUBIC_YARDS_PER_CUBIC_METER;
        }

        /// <summary>
        /// Loads the AGD file into a set of points
        /// </summary>
        /// <param name="filePath">Path and name of file to load</param>
        /// <param name="Field">Field to update</param>
        private void LoadTopologyData
            (
            string filePath,
            Field Field
            )
		{
			Field.TopologyPoints = new List<TopologyPoint>();

			string[] lines = File.ReadAllLines(filePath);

			for (int i = 1; i < lines.Length; i++) // Skip header
			{
				string[] fields = lines[i].Split(',');
				if (fields.Length >= 7)
				{
					TopologyPoint point = new TopologyPoint
					{
						Latitude = double.Parse(fields[0].Trim(), CultureInfo.InvariantCulture),
						Longitude = double.Parse(fields[1].Trim(), CultureInfo.InvariantCulture),
						Code = fields[5].Trim(),
						Comments = fields[6].Trim()
					};

					// Parse existing elevation if available
					if (!string.IsNullOrEmpty(fields[2].Trim()))
					{
						point.ExistingElevation = double.Parse(fields[2].Trim(), CultureInfo.InvariantCulture);
					}

					// Parse proposed elevation if available
					if (!string.IsNullOrEmpty(fields[3].Trim()))
					{
						point.ProposedElevation = double.Parse(fields[3].Trim(), CultureInfo.InvariantCulture);
					}

					// Only process points that have both existing and proposed elevations
					if (point.ExistingElevation != 0 && point.ProposedElevation != 0)
					{
						// Parse cut/fill if available
						if (!string.IsNullOrEmpty(fields[4].Trim()) && fields[4].Trim() != "0.000")
						{
							// agd file uses cut = negative but we use cut = positive
							point.CutFill = -double.Parse(fields[4].Trim(), CultureInfo.InvariantCulture);
						}
						else
						{
							// Calculate cut/fill from existing and proposed elevations
							// Positive = cut (existing > proposed), Negative = fill (existing < proposed)
							point.CutFill = point.ExistingElevation - point.ProposedElevation;
						}

						Field.TopologyPoints.Add(point);
					}
				}
			}
		}
	}
}
