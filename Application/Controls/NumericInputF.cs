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
    public partial class NumericInputF : UserControl
    {
        private uint _DecimalPlaces = 1;
        public uint DecimalPlaces
        {
            get { return _DecimalPlaces; }
            set { _DecimalPlaces = value; }
        }

        private double _Minimum = 0;
        public double Minimum
        {
            get { return _Minimum; }
            set { _Minimum = value; }
        }

        private double _Maximum = 2;
        public double Maximum
        {
            get { return _Maximum; }
            set { _Maximum = value; }
        }

        private double _ButtonChangeAmount = 0.1;
        public double ButtonChangeAmount
        {
            get { return _ButtonChangeAmount; }
            set { _ButtonChangeAmount = value; }
        }

        /// <summary>
        /// The control value
        /// </summary>
        public double Value
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

        public NumericInputF()
        {
            InitializeComponent();

            double ScalingFactor = this.DeviceDpi / 96.0;

            Bitmap scaledImage = new Bitmap(UpBtn.Image!, new Size((int)(UpBtn.Image!.Width * ScalingFactor), (int)(UpBtn.Image!.Height * ScalingFactor)));
            UpBtn.Image = scaledImage;
            scaledImage = new Bitmap(DownBtn.Image!, new Size((int)(DownBtn.Image!.Width * ScalingFactor), (int)(DownBtn.Image!.Height * ScalingFactor)));
            DownBtn.Image = scaledImage;

            ValueInput.Text = "0.0";
        }

        /// <summary>
        /// Shows a value
        /// </summary>
        /// <param name="Value">Value to show</param>
        private void ShowValue
            (
            double Value
            )
        {
            if (Value > Maximum) Value = Maximum;
            if (Value < Minimum) Value = Minimum;

            ValueInput.Text = Value.ToString("F" + DecimalPlaces.ToString());
        }

        /// <summary>
        /// Gets the value of the control
        /// </summary>
        /// <returns>Value or zero if invalid input</returns>
        private double GetValue
            (
            )
        {
            double Value;

            if (double.TryParse(ValueInput.Text, out Value))
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
            double Val = GetValue();

            ShowValue(Value + ButtonChangeAmount);
        }

        /// <summary>
        /// Called when user clicks on the down button
        /// Decreases the value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownBtn_Click(object sender, EventArgs e)
        {
            ShowValue(Value - ButtonChangeAmount);
        }

        /// <summary>
        /// Called when user enters a new value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ValueInput_TextChanged(object sender, EventArgs e)
        {
            if (double.TryParse(ValueInput.Text, out double NewValue))
            {
                ValueInput.BackColor = Color.White;
            }
            else
            {
                ValueInput.BackColor = Color.Pink;
            }
        }
    }
}
