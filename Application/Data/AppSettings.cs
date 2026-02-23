using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace AgGrade.Data
{
    public class AppSettings
    {
        public enum TractotColors
        {
            Red,
            Blue,
            Green,
            Yellow
        }

        [XmlIgnore]
        public IPAddress ControllerAddress;
        public int ControllerPort;
        public int LocalPort;
        [XmlIgnore]
        public IPAddress SubnetMask;
        public bool UseSecondaryTablet;
        public int MagneticDeclinationDegrees;
        public uint MagneticDeclinationMinutes;
        public bool LogData;
        public TractotColors TractorColor;

        // Property for XML serialization of IPAddress
        [XmlElement("ControllerAddress")]
        public string ControllerAddressString
        {
            get { return ControllerAddress?.ToString() ?? "0.0.0.0"; }
            set
            {
                if (IPAddress.TryParse(value, out IPAddress? address))
                {
                    ControllerAddress = address;
                }
                else
                {
                    ControllerAddress = new IPAddress(0);
                }
            }
        }

        // Property for XML serialization of SubnetMask
        [XmlElement("SubnetMask")]
        public string SubnetMaskString
        {
            get { return SubnetMask?.ToString() ?? "255.255.255.0"; }
            set
            {
                if (IPAddress.TryParse(value, out IPAddress? address))
                {
                    SubnetMask = address;
                }
                else
                {
                    SubnetMask = new IPAddress(new byte[] { 255, 255, 255, 0 });
                }
            }
        }

        public AppSettings
            (
            )
        {
            ControllerAddress = new IPAddress(new byte[] { 192, 168, 1, 1 });
            ControllerPort = 5000;
            SubnetMask = new IPAddress(new byte[] { 255, 255, 255, 0 });
            LocalPort = 5001;
            UseSecondaryTablet = false;
            MagneticDeclinationDegrees = 0;
            MagneticDeclinationMinutes = 0;
            LogData = true;
            TractorColor = TractotColors.Green;
        }

        /// <summary>
        /// Gets the path to the settings file in the application data directory
        /// </summary>
        private static string GetSettingsFilePath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataPath, "AgGrade");
            
            // Ensure the directory exists
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            return Path.Combine(appFolder, "AppSettings.xml");
        }

        /// <summary>
        /// Saves the settings to an XML file
        /// </summary>
        public void Save
            (
            )
        {
            try
            {
                string filePath = GetSettingsFilePath();
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fileStream, this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads the settings from an XML file
        /// </summary>
        public void Load
            (
            )
        {
            try
            {
                string filePath = GetSettingsFilePath();
                
                if (!File.Exists(filePath))
                {
                    // File doesn't exist, use default values
                    return;
                }
                
                XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    AppSettings? loadedSettings = serializer.Deserialize(fileStream) as AppSettings;
                    
                    if (loadedSettings != null)
                    {
                        ControllerAddress = loadedSettings.ControllerAddress;
                        ControllerPort = loadedSettings.ControllerPort;
                        SubnetMask = loadedSettings.SubnetMask;
                        LocalPort = loadedSettings.LocalPort;
                        UseSecondaryTablet = loadedSettings.UseSecondaryTablet;
                        MagneticDeclinationDegrees = loadedSettings.MagneticDeclinationDegrees;
                        MagneticDeclinationMinutes = loadedSettings.MagneticDeclinationMinutes;
                        LogData = loadedSettings.LogData;
                        TractorColor = loadedSettings.TractorColor;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
