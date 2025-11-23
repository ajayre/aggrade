using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Windows.Forms;

namespace AgGrade.Data
{
    public class PanSettings
    {
        public enum EndOfCuttingOptions
        {
            Float,
            Raise
        }

        public bool Equipped;
        public uint AntennaHeightCm;
        public uint WidthCm;
        public EndOfCuttingOptions EndofCutting;
        public uint RaiseHeightMm;

        public PanSettings
            (
            )
        {
            Equipped = true;
            AntennaHeightCm = 0;
            WidthCm = 0;
            EndofCutting = EndOfCuttingOptions.Raise;
            RaiseHeightMm = 0;
        }

        public XElement ToXml
            (
            )
        {
            return new XElement("PanSettings",
                new XElement("Equipped", Equipped),
                new XElement("AntennaHeightCm", AntennaHeightCm),
                new XElement("WidthCm", WidthCm),
                new XElement("EndofCutting", EndofCutting.ToString()),
                new XElement("RaiseHeightMm", RaiseHeightMm)
            );
        }

        public void FromXml
            (
            XElement xml
            )
        {
            if (xml == null)
            {
                return;
            }

            // Parse Equipped
            XElement? equippedElement = xml.Element("Equipped");
            if (equippedElement != null && bool.TryParse(equippedElement.Value, out bool equipped))
            {
                Equipped = equipped;
            }

            // Parse AntennaHeightCm
            XElement? antennaHeightElement = xml.Element("AntennaHeightCm");
            if (antennaHeightElement != null && uint.TryParse(antennaHeightElement.Value, out uint antennaHeight))
            {
                AntennaHeightCm = antennaHeight;
            }

            // Parse WidthCm
            XElement? widthElement = xml.Element("WidthCm");
            if (widthElement != null && uint.TryParse(widthElement.Value, out uint width))
            {
                WidthCm = width;
            }

            // Parse EndofCutting
            XElement? endOfCuttingElement = xml.Element("EndofCutting");
            if (endOfCuttingElement != null)
            {
                if (Enum.TryParse<EndOfCuttingOptions>(endOfCuttingElement.Value, out EndOfCuttingOptions endOfCutting))
                {
                    EndofCutting = endOfCutting;
                }
            }

            // Parse RaiseHeightMm
            XElement? raiseHeightElement = xml.Element("RaiseHeightMm");
            if (raiseHeightElement != null && uint.TryParse(raiseHeightElement.Value, out uint raiseHeight))
            {
                RaiseHeightMm = raiseHeight;
            }
        }
    }

    public class EquipmentSettings
    {
        public uint TractorAntennaHeightCm;
        public int TractorAntennaLeftOffsetCm;
        public int TractorAntennaForwardOffsetCm;
        public uint TractorTurningCircleFt;
        public uint TractorWidthCm;
        public PanSettings FrontPanSettings;
        public PanSettings RearPanSettings;

        public EquipmentSettings
            (
            )
        {
            TractorAntennaHeightCm = 0;
            TractorAntennaLeftOffsetCm = 0;
            TractorAntennaForwardOffsetCm = 0;
            TractorTurningCircleFt = 0;
            TractorWidthCm = 0;

            FrontPanSettings = new PanSettings();
            RearPanSettings = new PanSettings();
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
            
            return Path.Combine(appFolder, "EquipmentSettings.xml");
        }

        /// <summary>
        /// Saves settings to an XML file
        /// </summary>
        public void Save
            (
            )
        {
            try
            {
                string filePath = GetSettingsFilePath();
                
                XDocument doc = new XDocument(
                    new XElement("EquipmentSettings",
                        new XElement("TractorAntennaHeightCm", TractorAntennaHeightCm),
                        new XElement("TractorAntennaLeftOffsetCm", TractorAntennaLeftOffsetCm),
                        new XElement("TractorAntennaForwardOffsetCm", TractorAntennaForwardOffsetCm),
                        new XElement("TractorTurningCircleFt", TractorTurningCircleFt),
                        new XElement("TractorWidthCm", TractorWidthCm),
                        new XElement("FrontPanSettings", FrontPanSettings.ToXml().Elements()),
                        new XElement("RearPanSettings", RearPanSettings.ToXml().Elements())
                    )
                );
                
                doc.Save(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving equipment settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads settings from an XML file
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
                
                XDocument doc = XDocument.Load(filePath);
                XElement? root = doc.Element("EquipmentSettings");
                
                if (root == null)
                {
                    return;
                }
                
                // Parse TractorAntennaHeightCm
                XElement? tractorAntennaHeightElement = root.Element("TractorAntennaHeightCm");
                if (tractorAntennaHeightElement != null && uint.TryParse(tractorAntennaHeightElement.Value, out uint tractorAntennaHeight))
                {
                    TractorAntennaHeightCm = tractorAntennaHeight;
                }
                
                // Parse TractorAntennaLeftOffsetCm
                XElement? tractorAntennaLeftOffsetElement = root.Element("TractorAntennaLeftOffsetCm");
                if (tractorAntennaLeftOffsetElement != null && int.TryParse(tractorAntennaLeftOffsetElement.Value, out int tractorAntennaLeftOffset))
                {
                    TractorAntennaLeftOffsetCm = tractorAntennaLeftOffset;
                }
                
                // Parse TractorAntennaForwardOffsetCm
                XElement? tractorAntennaForwardOffsetElement = root.Element("TractorAntennaForwardOffsetCm");
                if (tractorAntennaForwardOffsetElement != null && int.TryParse(tractorAntennaForwardOffsetElement.Value, out int tractorAntennaForwardOffset))
                {
                    TractorAntennaForwardOffsetCm = tractorAntennaForwardOffset;
                }
                
                // Parse TractorTurningCircleFt
                XElement? tractorTurningCircleElement = root.Element("TractorTurningCircleFt");
                if (tractorTurningCircleElement != null && uint.TryParse(tractorTurningCircleElement.Value, out uint tractorTurningCircle))
                {
                    TractorTurningCircleFt = tractorTurningCircle;
                }
                
                // Parse TractorWidthCm
                XElement? tractorWidthElement = root.Element("TractorWidthCm");
                if (tractorWidthElement != null && uint.TryParse(tractorWidthElement.Value, out uint tractorWidth))
                {
                    TractorWidthCm = tractorWidth;
                }
                
                // Parse FrontPanSettings
                XElement? frontPanSettingsElement = root.Element("FrontPanSettings");
                if (frontPanSettingsElement != null)
                {
                    // Create a wrapper XElement with "PanSettings" as the name for FromXml
                    XElement frontPanXml = new XElement("PanSettings", frontPanSettingsElement.Elements());
                    FrontPanSettings.FromXml(frontPanXml);
                }
                
                // Parse RearPanSettings
                XElement? rearPanSettingsElement = root.Element("RearPanSettings");
                if (rearPanSettingsElement != null)
                {
                    // Create a wrapper XElement with "PanSettings" as the name for FromXml
                    XElement rearPanXml = new XElement("PanSettings", rearPanSettingsElement.Elements());
                    RearPanSettings.FromXml(rearPanXml);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
