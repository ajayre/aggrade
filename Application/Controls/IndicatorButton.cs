using OpenCvSharp.Internal.Vectors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Timer = System.Windows.Forms.Timer;

namespace AgGrade.Controls
{
    public partial class IndicatorButton : UserControl
    {
        public delegate void ButtonClicked(object sender, EventArgs e);
        public event ButtonClicked OnButtonClicked = null;

        private Timer FlashTimer;
        private bool FlashState;

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

        private IndicatorColor _Indicator;
        public IndicatorColor Indicator
        {
            get
            {
                return _Indicator;
            }

            set
            {
                _Indicator = value;

                StopFlashing();

                if (value == IndicatorColor.Red)
                {
                    IndicatorPanel.BackColor = Color.Red;
                    StartFlashing();
                }
                else if (value == IndicatorColor.Orange)
                {
                    IndicatorPanel.BackColor = Color.Orange;
                }
                else if (value == IndicatorColor.Off)
                {
                    IndicatorPanel.BackColor = Color.Gray;
                }
                else
                {
                    IndicatorPanel.BackColor = Color.LightGreen;
                }
            }
        }

        public IndicatorButton()
        {
            InitializeComponent();

            FlashTimer = new Timer();
            FlashTimer.Interval = 500;
            FlashTimer.Tick += FlashTimer_Tick;
        }

        /// <summary>
        /// Called periodically to flash the indicator
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlashTimer_Tick(object? sender, EventArgs e)
        {
            if (FlashState)
            {
                IndicatorPanel.BackColor = Color.Red;
                FlashState = false;
            }
            else
            {
                IndicatorPanel.BackColor = Color.Gray;
                FlashState = true;
            }
        }

        /// <summary>
        /// Start flashing the indicator
        /// </summary>
        private void StartFlashing
            (
            )
        {
            FlashState = false;
            FlashTimer.Start();
        }

        /// <summary>
        /// Stop flashing the indicator
        /// </summary>
        private void StopFlashing
            (
            )
        {
            FlashTimer.Stop();
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

        private void Btn_Click(object sender, EventArgs e)
        {
            OnButtonClicked?.Invoke(sender, e);
        }
    }
}
