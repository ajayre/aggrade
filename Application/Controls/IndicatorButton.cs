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
    public partial class IndicatorButton : UserControl
    {
        public enum IndicatorColor
        {
            Red,
            Orange,
            Green,
            Off
        }

        public Image? Image
        {
            get
            {
                return Btn.Image;
            }

            set
            {
                Btn.Image = value;
                ScaleImage();
            }
        }

        public IndicatorColor Indicator
        {
            get
            {
                if (IndicatorPanel.BackColor == Color.Red) return IndicatorColor.Red;
                else if (IndicatorPanel.BackColor == Color.Orange) return IndicatorColor.Orange;
                else if (IndicatorPanel.BackColor == Color.Gray) return IndicatorColor.Off;
                else return IndicatorColor.Green;
            }

            set
            {
                if (value == IndicatorColor.Red) IndicatorPanel.BackColor = Color.Red;
                else if (value == IndicatorColor.Orange) IndicatorPanel.BackColor = Color.Orange;
                else if (value == IndicatorColor.Off) IndicatorPanel.BackColor = Color.Gray;
                else IndicatorPanel.BackColor = Color.LightGreen;
            }
        }

        public IndicatorButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Scales the image to match the display DPI
        /// </summary>
        private void ScaleImage
            (
            )
        {
            double ScalingFactor = this.DeviceDpi / 96.0;

            if (Btn.Image != null)
            {
                Bitmap scaledImage = new Bitmap(Btn.Image!, new Size((int)(Btn.Image!.Width * ScalingFactor), (int)(Btn.Image!.Height * ScalingFactor)));
                Btn.Image = scaledImage;
            }
        }
    }
}
