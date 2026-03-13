using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class CalibrationPage : UserControl
    {
        public CalibrationPage()
        {
            InitializeComponent();

            OptionsTable.Controls.Clear();

            // Add in reverse order so first sorted item appears at top (DockStyle.Top stacks first-added at bottom)
            bool odd = false;

            AddButton("Calibrate Tractor Antenna Location", Properties.Resources.tractor_48px, odd, CalibrateTractorAntenna);
            odd = !odd;
            AddButton("Calibrate Rear Blade Height", Properties.Resources.blade_48px, odd, CalibrateRearBladeHeight);
            odd = !odd;
            AddButton("Calibrate Front Blade Height", Properties.Resources.blade_48px, odd, CalibrateFrontBladeHeight);
            odd = !odd;
            AddButton("Calibrate Field Location", Properties.Resources.field_48px, odd, CalibrateFieldLocation);
            odd = !odd;
        }

        /// <summary>
        /// Called when user taps on the button to calibrate the field location
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateFieldLocation
            (
            object Sender
            )
        {

        }

        /// <summary>
        /// Called when user taps on the button to calibrate the tractor antenna
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateTractorAntenna
            (
            object Sender
            )
        {

        }

        /// <summary>
        /// Called when the user taps on the button to calibrate the front blade height
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateFrontBladeHeight
            (
            object Sender
            )
        {

        }

        /// <summary>
        /// Called when the user taps on the button to calibrate the rear blade height
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateRearBladeHeight
            (
            object Sender
            )
        {

        }

        /// <summary>
        /// Adds a new button
        /// </summary>
        /// <param name="Text">Text to display on button</param>
        /// <param name="Icon">Icon to show</param>
        /// <param name="Odd">true to use secondary color</param>
        /// <param name="OnClicked">Handler when button is clicked/tapped</param>
        private void AddButton
            (
            string Text,
            Image? Icon,
            bool Odd,
            Action<object> OnClicked
            )
        {
            var panel = new ButtonPanel();
            panel.OnClicked += OnClicked;
            panel.Odd = Odd;
            panel.CaptionText = Text;
            panel.DisplayIcon = Icon;
            panel.Dock = DockStyle.Top;
            OptionsTable.Controls.Add(panel);
        }
    }
}
