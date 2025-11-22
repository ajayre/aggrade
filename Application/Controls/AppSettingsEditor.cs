using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using AgGrade.Data;

namespace AgGrade.Controls
{
    public partial class AppSettingsEditor : UserControl
    {
        public delegate void PowerOff();
        public event PowerOff OnPowerOff;

        public AppSettingsEditor()
        {
            InitializeComponent();

            double ScalingFactor = this.DeviceDpi / 96.0;

            Bitmap scaledImage = new Bitmap(PowerBtn.Image!, new Size((int)(PowerBtn.Image!.Width * ScalingFactor), (int)(PowerBtn.Image!.Height * ScalingFactor)));
            PowerBtn.Image = scaledImage;
        }

        /// <summary>
        /// Called when user taps on power off button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerBtn_Click(object sender, EventArgs e)
        {
            OnPowerOff?.Invoke();
        }

        /// <summary>
        /// Validates an IP address octet value (0-255)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <param name="fieldName">Name of the field for error messages</param>
        /// <returns>The validated byte value</returns>
        /// <exception cref="ArgumentException">Thrown if value is out of range</exception>
        private byte ValidateIPOctet(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{fieldName} cannot be empty.");
            }

            if (!byte.TryParse(value, out byte octet))
            {
                throw new ArgumentException($"{fieldName} must be a valid number between 0 and 255.");
            }

            return octet;
        }

        /// <summary>
        /// Validates a port number (0-65535)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>The validated port number</returns>
        /// <exception cref="ArgumentException">Thrown if value is out of range</exception>
        private int ValidatePort(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Port number cannot be empty.");
            }

            if (!int.TryParse(value, out int port))
            {
                throw new ArgumentException("Port number must be a valid number between 0 and 65535.");
            }

            if (port < 0 || port > 65535)
            {
                throw new ArgumentException("Port number must be between 0 and 65535.");
            }

            return port;
        }

        /// <summary>
        /// Validates magnetic declination degrees (can be positive or negative)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>The validated degrees value</returns>
        /// <exception cref="ArgumentException">Thrown if value is invalid</exception>
        private int ValidateMagneticDeclinationDegrees(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Magnetic declination degrees cannot be empty.");
            }

            if (!int.TryParse(value, out int degrees))
            {
                throw new ArgumentException("Magnetic declination degrees must be a valid integer.");
            }

            return degrees;
        }

        /// <summary>
        /// Validates magnetic declination minutes (must be positive, 0 or greater)
        /// </summary>
        /// <param name="value">The value to validate</param>
        /// <returns>The validated minutes value</returns>
        /// <exception cref="ArgumentException">Thrown if value is invalid or negative</exception>
        private uint ValidateMagneticDeclinationMinutes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Magnetic declination minutes cannot be empty.");
            }

            if (!uint.TryParse(value, out uint minutes))
            {
                throw new ArgumentException("Magnetic declination minutes must be a valid non-negative number.");
            }

            return minutes;
        }

        /// <summary>
        /// Gets the current settings from the UI with validation
        /// </summary>
        /// <returns>Current settings</returns>
        /// <exception cref="ArgumentException">Thrown if any input validation fails</exception>
        public AppSettings GetSettings
            (
            )
        {
            AppSettings Settings = new AppSettings();

            // Validate and parse IP address octets
            byte octet1 = ValidateIPOctet(ControllerIPAddrNum1.Text, "IP Address octet 1");
            byte octet2 = ValidateIPOctet(ControllerIPAddrNum2.Text, "IP Address octet 2");
            byte octet3 = ValidateIPOctet(ControllerIPAddrNum3.Text, "IP Address octet 3");
            byte octet4 = ValidateIPOctet(ControllerIPAddrNum4.Text, "IP Address octet 4");

            // Create IP address from octets
            Settings.ControllerAddress = new IPAddress(new byte[] { octet1, octet2, octet3, octet4 });

            // Validate and parse subnet mask octets
            byte subnetOctet1 = ValidateIPOctet(SubnetMaskNum1.Text, "Subnet Mask octet 1");
            byte subnetOctet2 = ValidateIPOctet(SubnetMaskNum2.Text, "Subnet Mask octet 2");
            byte subnetOctet3 = ValidateIPOctet(SubnetMaskNum3.Text, "Subnet Mask octet 3");
            byte subnetOctet4 = ValidateIPOctet(SubnetMaskNum4.Text, "Subnet Mask octet 4");

            // Create subnet mask from octets
            Settings.SubnetMask = new IPAddress(new byte[] { subnetOctet1, subnetOctet2, subnetOctet3, subnetOctet4 });

            // Validate and parse port number
            Settings.ControllerPort = ValidatePort(ControllerPortNum.Text);

            // Parse secondary tablet selector (ComboBox: 0 = "No", 1 = "Yes")
            Settings.UseSecondaryTablet = SecondaryTabletSelector.SelectedIndex == 1;

            // Validate and parse magnetic declination degrees (can be positive or negative)
            Settings.MagneticDeclinationDegrees = ValidateMagneticDeclinationDegrees(MagDeclinationDeg.Text);

            // Validate and parse magnetic declination minutes (must be positive)
            Settings.MagneticDeclinationMinutes = ValidateMagneticDeclinationMinutes(MagDeclinationMin.Text);

            // Parse log data selector (ComboBox: 0 = "No", 1 = "Yes")
            Settings.LogData = LogDataSelector.SelectedIndex == 1;

            return Settings;
        }

        /// <summary>
        /// Displays the settings in the UI controls
        /// </summary>
        /// <param name="Settings">The settings to display</param>
        public void ShowSettings
            (
            AppSettings Settings
            )
        {
            // Display IP address octets
            if (Settings.ControllerAddress != null)
            {
                byte[] addressBytes = Settings.ControllerAddress.GetAddressBytes();
                if (addressBytes.Length >= 4)
                {
                    ControllerIPAddrNum1.Text = addressBytes[0].ToString();
                    ControllerIPAddrNum2.Text = addressBytes[1].ToString();
                    ControllerIPAddrNum3.Text = addressBytes[2].ToString();
                    ControllerIPAddrNum4.Text = addressBytes[3].ToString();
                }
                else
                {
                    ControllerIPAddrNum1.Text = "0";
                    ControllerIPAddrNum2.Text = "0";
                    ControllerIPAddrNum3.Text = "0";
                    ControllerIPAddrNum4.Text = "0";
                }
            }
            else
            {
                ControllerIPAddrNum1.Text = "0";
                ControllerIPAddrNum2.Text = "0";
                ControllerIPAddrNum3.Text = "0";
                ControllerIPAddrNum4.Text = "0";
            }

            // Display subnet mask octets
            if (Settings.SubnetMask != null)
            {
                byte[] subnetBytes = Settings.SubnetMask.GetAddressBytes();
                if (subnetBytes.Length >= 4)
                {
                    SubnetMaskNum1.Text = subnetBytes[0].ToString();
                    SubnetMaskNum2.Text = subnetBytes[1].ToString();
                    SubnetMaskNum3.Text = subnetBytes[2].ToString();
                    SubnetMaskNum4.Text = subnetBytes[3].ToString();
                }
                else
                {
                    SubnetMaskNum1.Text = "255";
                    SubnetMaskNum2.Text = "255";
                    SubnetMaskNum3.Text = "255";
                    SubnetMaskNum4.Text = "0";
                }
            }
            else
            {
                SubnetMaskNum1.Text = "255";
                SubnetMaskNum2.Text = "255";
                SubnetMaskNum3.Text = "255";
                SubnetMaskNum4.Text = "0";
            }

            // Display port number
            ControllerPortNum.Text = Settings.ControllerPort.ToString();

            // Display secondary tablet selector
            SecondaryTabletSelector.SelectedIndex = Settings.UseSecondaryTablet ? 1 : 0;

            // Display magnetic declination degrees
            MagDeclinationDeg.Text = Settings.MagneticDeclinationDegrees.ToString();

            // Display magnetic declination minutes
            MagDeclinationMin.Text = Settings.MagneticDeclinationMinutes.ToString();

            // Display log data selector
            LogDataSelector.SelectedIndex = Settings.LogData ? 1 : 0;
        }
    }
}
