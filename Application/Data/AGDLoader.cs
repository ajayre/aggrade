using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
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
        private const double ACCURACY_TOLERANCE_M = 0.03048; // 0.1ft in meters

        public List<TopologyPoint> TopologyPoints { get; private set; }
        public double FieldCentroidLat { get; private set; }
		public double FieldCentroidLon { get; private set; }
        public double FieldMinX { get; private set; }
        public double FieldMinY { get; private set; }
        public List<Bin> Bins { get; private set; }
        public int UTMZone { get; private set; }
        public bool IsNorthernHemisphere { get; private set; }
        public double TotalCutBCY { get; private set; }
        public double TotalFillBCY { get; private set; }

        public AGDLoader
			(
			)
		{
			TopologyPoints = new List<TopologyPoint>();
            //CutFillMap = new Dictionary<Point, double>();
            Bins = new List<Bin>();
		}

        /// <summary>
        /// Loads in an AGD (Optisurface) file
        /// </summary>
        /// <param name="FileName"></param>
        public void Load
			(
			string FileName
			)
		{
			TopologyPoints = LoadTopologyData(FileName);

            CalculateSiteCentroid();
            
			ConvertToLocalCoordinates();
			
			// create 2ft x 2ft bins
            CalculateCutFillVolumesWithBinning();
        }

		/// <summary>
		/// Gets the lat,lon for the center of the field
		/// </summary>
		private void CalculateSiteCentroid
			(
			)
		{
            double TotalLat = 0;
            double TotalLon = 0;
            int Count = 0;

            foreach (TopologyPoint Point in TopologyPoints)
            {
                if (Point.ExistingElevation != 0 || Point.ProposedElevation != 0)
                {
                    TotalLat += Point.Latitude;
                    TotalLon += Point.Longitude;
                    Count++;
                }
            }

            FieldCentroidLat = TotalLat / Count;
            FieldCentroidLon = TotalLon / Count;
        }

		/// <summary>
		/// Gets the X,Y for SW corner of field
		/// Gets the X,Y for each point in the topology
		/// </summary>
        private void ConvertToLocalCoordinates()
        {
            // find the Southwest corner (minimum X and Y) to use as origin
            double MinLat = TopologyPoints.Min(p => p.Latitude);
            double MinLon = TopologyPoints.Min(p => p.Longitude);

			UTM.UTMCoordinate MinXY = UTM.FromLatLon(MinLat, MinLon);
			FieldMinX = MinXY.Easting;
			FieldMinY = MinXY.Northing;

            UTMZone = MinXY.Zone;
            IsNorthernHemisphere = MinXY.IsNorthernHemisphere;

            foreach (TopologyPoint Point in TopologyPoints)
            {
				UTM.UTMCoordinate Coord = UTM.FromLatLon(Point.Latitude, Point.Longitude);
				Point.X = Coord.Easting;
				Point.Y = Coord.Northing;
            }
        }

        /// <summary>
        /// Divides the field up into 2ft x 2ft bins and calculates the cut and fill volume for each bin
        /// </summary>
        private void CalculateCutFillVolumesWithBinning()
        {
            // Create 2ft x 2ft bins (2ft = 0.6096m)
            double BinSizeM = 0.6096;

            // Find bounds of the field
            double MinX = TopologyPoints.Min(p => p.X);
            double MaxX = TopologyPoints.Max(p => p.X);
            double MinY = TopologyPoints.Min(p => p.Y);
            double MaxY = TopologyPoints.Max(p => p.Y);

            // get size of field in bins
            int BinWidth = (int)Math.Ceiling((MaxX - MinX) / BinSizeM);
            int BinHeight = (int)Math.Ceiling((MaxY - MinY) / BinSizeM);

            // Initialize bins
            Dictionary<Point, List<double>> BinCutFillValues = new Dictionary<Point, List<double>>();
            Dictionary<Point, double> CutFillMap = new Dictionary<Point, double>();
            Dictionary<Point, List<double>> BinExistingElevationValues = new Dictionary<Point, List<double>>();
            Dictionary<Point, List<double>> BinTargetElevationValues = new Dictionary<Point, List<double>>();
            Bins.Clear();

            int ProcessedPoints = 0;
            int TotalPoints = TopologyPoints.Count;

            // Bin the AGD data points
            foreach (TopologyPoint Point in TopologyPoints)
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
                    UTM.ToLatLon(UTMZone, IsNorthernHemisphere, BinMinX, BinMinY, out Lat, out Lon);
                    NewBin.SouthwestCorner = new Coordinate() { Latitude = Lat, Longitude = Lon };

                    UTM.ToLatLon(UTMZone, IsNorthernHemisphere, BinMaxX, BinMaxY, out Lat, out Lon);
                    NewBin.NortheastCorner = new Coordinate() { Latitude = Lat, Longitude = Lon };

                    UTM.ToLatLon(UTMZone, IsNorthernHemisphere, CentroidX, CentroidY, out Lat, out Lon);
                    NewBin.Centroid = new Coordinate() { Latitude = Lat, Longitude = Lon };

                    Bins.Add(NewBin);
                }
            }

            // Calculate volumes using proper 2ft x 2ft bin areas
            double TotalCutVolumeM3 = 0;
            double TotalFillVolumeM3 = 0;
            double BinAreaM2 = BinSizeM * BinSizeM; // 0.3716 m² per bin (4 ft²)

            foreach (Bin b in Bins)
            {
                TotalCutVolumeM3 += (b.CutAmountM * BinAreaM2);
                TotalFillVolumeM3 += (b.FillAmountM * BinAreaM2);
            }

            TotalCutBCY = TotalCutVolumeM3 * CUBIC_YARDS_PER_CUBIC_METER;
            TotalFillBCY = TotalFillVolumeM3 * CUBIC_YARDS_PER_CUBIC_METER;
        }

        /// <summary>
        /// Loads the AGD file into a set of points
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<TopologyPoint> LoadTopologyData(string filePath)
		{
			var topologyPoints = new List<TopologyPoint>();

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

						topologyPoints.Add(point);
					}
				}
			}

			return topologyPoints;
		}
	}
}

