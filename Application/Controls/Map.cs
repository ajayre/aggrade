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

        /// <summary>
        /// pixels per meter
        /// </summary>
        public double ScaleFactor { get; private set; }


        public Map()
        {
            InitializeComponent();

            MapGen = new MapGenerator();
        }

        public void ShowField
            (
            Field Field
            )
        {
            CurrentField = Field;

            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height);

            // fixme - remove
            //ScaleFactor = 200;

            RefreshMap();
        }

        private void RefreshMap
            (
            )
        {
            double Lat = 36.446847109944279;
            double Lon = -90.72286177445794;

            //Haversine.MoveDistanceBearing(ref Lat, ref Lon, 0, 20);

            //MapCanvas.Image = MapGen.GenerateZoomToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, false);
            MapCanvas.Image = MapGen.Generate(CurrentField, MapCanvas.Width, MapCanvas.Height, false, ScaleFactor,
                Lat, Lon, 45);
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
                ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height);
                RefreshMap();
            }
        }
    }
}
