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
        private MapGenerator MapGen;

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
            Bitmap Map = MapGen.Generate(Field, false, 3);
            MapCanvas.Image = Map;
        }
    }
}
