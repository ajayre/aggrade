using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class NumericInput : UserControl
    {
        public NumericInput()
        {
            InitializeComponent();

            double ScalingFactor = this.DeviceDpi / 96.0;

            Bitmap scaledImage = new Bitmap(UpBtn.Image!, new Size((int)(UpBtn.Image!.Width * ScalingFactor), (int)(UpBtn.Image!.Height * ScalingFactor)));
            UpBtn.Image = scaledImage;
            scaledImage = new Bitmap(DownBtn.Image!, new Size((int)(DownBtn.Image!.Width * ScalingFactor), (int)(DownBtn.Image!.Height * ScalingFactor)));
            DownBtn.Image = scaledImage;
        }
    }
}
