using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AgGrade.Data;

namespace AgGrade.Controls
{
    public partial class Map : UserControl
    {
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
            ScaleFactor = 6.56;
        }

        public void ShowField
            (
            Field Field
            )
        {
            CurrentField = Field;

            ScaleFactor = MapGenerator.CalculateScaleFactorToFit(CurrentField, MapCanvas.Width, MapCanvas.Height);

            RefreshMap();
        }

        private void RefreshMap
            (
            )
        {
            //MapCanvas.Image = MapGen.GenerateZoomToFit(CurrentField, MapCanvas.Width, MapCanvas.Height, false);
            MapCanvas.Image = MapGen.Generate(CurrentField, MapCanvas.Width, MapCanvas.Height, false, ScaleFactor,
                36.446847109944279, -90.72286177445794, 45);
        }

        /// <summary>
        /// Zoom into the map
        /// </summary>
        public void ZoomIn
            (
            )
        {
            // fixme - add limit
            ScaleFactor *= 2;
            RefreshMap();
        }

        /// <summary>
        /// Zoom out of the map
        /// </summary>
        public void ZoomOut
            (
            )
        {
            // fixme - add limit
            ScaleFactor /= 2;
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
