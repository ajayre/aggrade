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
        /// <summary>
        /// The control value
        /// </summary>
        public int Value
        {
            get
            {
                return GetValue();
            }

            set
            {
                ShowValue(value);
            }
        }

        private bool _Unsigned = false;
        public bool Unsigned
        {
            get { return _Unsigned; }
            set { _Unsigned = value; }
        }

        public NumericInput()
        {
            InitializeComponent();

            double ScalingFactor = this.DeviceDpi / 96.0;

            Bitmap scaledImage = new Bitmap(UpBtn.Image!, new Size((int)(UpBtn.Image!.Width * ScalingFactor), (int)(UpBtn.Image!.Height * ScalingFactor)));
            UpBtn.Image = scaledImage;
            scaledImage = new Bitmap(DownBtn.Image!, new Size((int)(DownBtn.Image!.Width * ScalingFactor), (int)(DownBtn.Image!.Height * ScalingFactor)));
            DownBtn.Image = scaledImage;

            ValueInput.Text = "0";
        }

        /// <summary>
        /// Shows a value
        /// </summary>
        /// <param name="Value">Value to show</param>
        private void ShowValue
            (
            int Value
            )
        {
            ValueInput.Text = Value.ToString();
        }

        /// <summary>
        /// Gets the value of the control
        /// </summary>
        /// <returns>Value or zero if invalid input</returns>
        private int GetValue
            (
            )
        {
            int Value;

            if (int.TryParse(ValueInput.Text, out Value))
            {
                return Value;
            }

            return 0;
        }

        /// <summary>
        /// Called when user clicks on the up button
        /// Increases the value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpBtn_Click(object sender, EventArgs e)
        {
            int Val = GetValue();

            if (Unsigned && Val < 0)
            {
                Value = 1;
            }
            else
            {
                ShowValue(Val + 1);
            }
        }

        /// <summary>
        /// Called when user clicks on the down button
        /// Decreases the value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownBtn_Click(object sender, EventArgs e)
        {
            int Val = GetValue();
            if (Unsigned && Val <= 0)
            {
                Val = 1;
            }

            ShowValue(Val - 1);
        }

        /// <summary>
        /// Called when user enters a new value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValueInput_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(ValueInput.Text, out int NewValue))
            {
                if (Unsigned && NewValue < 0)
                {
                    ValueInput.BackColor = Color.Pink;
                }
                else
                {
                    ValueInput.BackColor = Color.White;
                }
            }
            else
            {
                ValueInput.BackColor = Color.Pink;
            }
        }
    }
}
