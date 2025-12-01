using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AgGrade.Controller;
using AgGrade.Data;

namespace AgGrade.Controls
{
    public partial class Map : UserControl
    {
        private const double MIN_SCALE_FACTOR = 0.5;
        private const double MAX_SCALE_FACTOR = 750.0;

        private Field CurrentField;
        private MapGenerator MapGen;
        private double TractorHeading;
        private Coordinate TractorLocation;

        /// <summary>
        /// pixels per meter
        /// </summary>
        public double ScaleFactor { get; private set; }

        public Map()
        {
            InitializeComponent();

            MapGen = new MapGenerator();
            MapGen.TractorColor = MapGenerator.TractorColors.Red;
            MapGen.TractorYOffset = 7;

            TractorLocation = new Coordinate();
            TractorLocation.Latitude = 36.446847109944279;
            TractorLocation.Longitude = -90.72286177445794;
        }

        public void ShowField
            (
            Field Field
            )
        {
            CurrentField = Field;

            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading);

            RefreshMap();
        }

        /// <summary>
        /// Sets the tractor location and heading
        /// </summary>
        /// <param name="Latitude">New latitude</param>
        /// <param name="Longitude">New longitude</param>
        /// <param name="Heading">New heading in degrees</param>
        public void SetTractor
            (
            double Latitude,
            double Longitude,
            double Heading
            )
        {
            TractorLocation.Latitude = Latitude;
            TractorLocation.Longitude = Longitude;
            TractorHeading = Heading;
            RefreshMap();
        }

        /// <summary>
        /// Sets the tractor location
        /// </summary>
        /// <param name="Latitude">New latitude</param>
        /// <param name="Longitude">New longitude</param>
        public void SetTractorLocation
            (
            double Latitude,
            double Longitude
            )
        {
            TractorLocation.Latitude = Latitude;
            TractorLocation.Longitude = Longitude;
            RefreshMap();
        }

        /// <summary>
        /// Sets the tractor heading
        /// </summary>
        /// <param name="NewHeading">New heading in degrees</param>
        public void SetTractorHeading
            (
            double Heading
            )
        {
            TractorHeading = Heading;
            RefreshMap();
        }

        private void RefreshMap
            (
            )
        {
            //Haversine.MoveDistanceBearing(ref Lat, ref Lon, 0, 20);

            //MapCanvas.Image = MapGen.GenerateZoomToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, false);
            MapCanvas.Image = MapGen.Generate(CurrentField, MapCanvas.Width, MapCanvas.Height, false, ScaleFactor,
                TractorLocation.Latitude, TractorLocation.Longitude, TractorHeading);
        }

        /// <summary>
        /// Zooms the map to fit
        /// </summary>
        public void ZoomToFit
            (
            )
        {
            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading);

            RefreshMap();
        }

        /// <summary>
        /// Zoom into the map
        /// </summary>
        public void ZoomIn
            (
            )
        {
            if (ScaleFactor * 2 <= MAX_SCALE_FACTOR)
            {
                ScaleFactor *= 2;
            }
            RefreshMap();
        }

        /// <summary>
        /// Zoom out of the map
        /// </summary>
        public void ZoomOut
            (
            )
        {
            if (ScaleFactor / 2 >= MIN_SCALE_FACTOR)
            {
                ScaleFactor /= 2;
            }
            RefreshMap();
        }

        private void MapCanvas_SizeChanged(object sender, EventArgs e)
        {
            if (CurrentField != null)
            {
                ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, TractorHeading);
                RefreshMap();
            }
        }
    }
}
