using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;
using System.Windows.Forms;
using AgGrade.Controller;

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
        public uint AntennaHeightMm;
        public uint WidthMm;
        public EndOfCuttingOptions EndofCutting;
        public uint RaiseHeightMm;
        public uint MaxCutDepthMm;
        public uint CapacityCY;

        public PanSettings
            (
            )
        {
            Equipped = true;
            AntennaHeightMm = 0;
            WidthMm = 0;
            EndofCutting = EndOfCuttingOptions.Raise;
            RaiseHeightMm = 0;
            MaxCutDepthMm = 61;
            CapacityCY = 8;
        }

        public XElement ToXml
            (
            )
        {
            return new XElement("PanSettings",
                new XElement("Equipped", Equipped),
                new XElement("AntennaHeightMm", AntennaHeightMm),
                new XElement("WidthMm", WidthMm),
                new XElement("EndofCutting", EndofCutting.ToString()),
                new XElement("RaiseHeightMm", RaiseHeightMm),
                new XElement("MaxCutDepthMm", MaxCutDepthMm),
                new XElement("CapacityCY", CapacityCY)
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

            // Parse AntennaHeightMm
            XElement? antennaHeightElement = xml.Element("AntennaHeightMm");
            if (antennaHeightElement != null && uint.TryParse(antennaHeightElement.Value, out uint antennaHeight))
            {
                AntennaHeightMm = antennaHeight;
            }

            // Parse WidthMm
            XElement? widthElement = xml.Element("WidthMm");
            if (widthElement != null && uint.TryParse(widthElement.Value, out uint width))
            {
                WidthMm = width;
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

            // Parse MaxCutDepthMm
            XElement? maxCutDepthElement = xml.Element("MaxCutDepthMm");
            if (maxCutDepthElement != null && uint.TryParse(maxCutDepthElement.Value, out uint maxCutDepth))
            {
                MaxCutDepthMm = maxCutDepth;
            }

            // Parse CapacityCY
            XElement? capacityCYElement = xml.Element("CapacityCY");
            if (capacityCYElement != null && uint.TryParse(capacityCYElement.Value, out uint capacityCY))
            {
                CapacityCY = capacityCY;
            }
        }
    }

    public class EquipmentSettings
    {
        public uint TractorAntennaHeightMm;
        public int TractorAntennaLeftOffsetMm;
        public int TractorAntennaForwardOffsetMm;
        public uint TractorTurningCircleM;
        public uint TractorWidthMm;
        public PanSettings FrontPan;
        public PanSettings RearPan;
        public BladeConfiguration FrontBlade;
        public BladeConfiguration RearBlade;

        public EquipmentSettings
            (
            )
        {
            TractorAntennaHeightMm = 0;
            TractorAntennaLeftOffsetMm = 0;
            TractorAntennaForwardOffsetMm = 0;
            TractorTurningCircleM = 0;
            TractorWidthMm = 0;

            FrontPan = new PanSettings();
            RearPan = new PanSettings();

            FrontBlade = new BladeConfiguration();
            RearBlade = new BladeConfiguration();
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
                        new XElement("TractorAntennaHeightMm", TractorAntennaHeightMm),
                        new XElement("TractorAntennaLeftOffsetMm", TractorAntennaLeftOffsetMm),
                        new XElement("TractorAntennaForwardOffsetMm", TractorAntennaForwardOffsetMm),
                        new XElement("TractorTurningCircleM", TractorTurningCircleM),
                        new XElement("TractorWidthMm", TractorWidthMm),
                        new XElement("FrontPanSettings", FrontPan.ToXml().Elements()),
                        new XElement("RearPanSettings", RearPan.ToXml().Elements()),
                        new XElement("FrontBladeSettings", FrontBlade.ToXml().Elements()),
                        new XElement("RearBladeSettings", RearBlade.ToXml().Elements())
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
                
                // Parse TractorAntennaHeightMm
                XElement? tractorAntennaHeightElement = root.Element("TractorAntennaHeightMm");
                if (tractorAntennaHeightElement != null && uint.TryParse(tractorAntennaHeightElement.Value, out uint tractorAntennaHeight))
                {
                    TractorAntennaHeightMm = tractorAntennaHeight;
                }
                
                // Parse TractorAntennaLeftOffsetMm
                XElement? tractorAntennaLeftOffsetElement = root.Element("TractorAntennaLeftOffsetMm");
                if (tractorAntennaLeftOffsetElement != null && int.TryParse(tractorAntennaLeftOffsetElement.Value, out int tractorAntennaLeftOffset))
                {
                    TractorAntennaLeftOffsetMm = tractorAntennaLeftOffset;
                }
                
                // Parse TractorAntennaForwardOffsetMm
                XElement? tractorAntennaForwardOffsetElement = root.Element("TractorAntennaForwardOffsetMm");
                if (tractorAntennaForwardOffsetElement != null && int.TryParse(tractorAntennaForwardOffsetElement.Value, out int tractorAntennaForwardOffset))
                {
                    TractorAntennaForwardOffsetMm = tractorAntennaForwardOffset;
                }
                
                // Parse TractorTurningCircleM
                XElement? tractorTurningCircleElement = root.Element("TractorTurningCircleM");
                if (tractorTurningCircleElement != null && uint.TryParse(tractorTurningCircleElement.Value, out uint tractorTurningCircle))
                {
                    TractorTurningCircleM = tractorTurningCircle;
                }
                
                // Parse TractorWidthMm
                XElement? tractorWidthElement = root.Element("TractorWidthMm");
                if (tractorWidthElement != null && uint.TryParse(tractorWidthElement.Value, out uint tractorWidth))
                {
                    TractorWidthMm = tractorWidth;
                }
                
                // Parse FrontPanSettings
                XElement? frontPanSettingsElement = root.Element("FrontPanSettings");
                if (frontPanSettingsElement != null)
                {
                    // Create a wrapper XElement with "PanSettings" as the name for FromXml
                    XElement frontPanXml = new XElement("PanSettings", frontPanSettingsElement.Elements());
                    FrontPan.FromXml(frontPanXml);
                }
                
                // Parse RearPanSettings
                XElement? rearPanSettingsElement = root.Element("RearPanSettings");
                if (rearPanSettingsElement != null)
                {
                    // Create a wrapper XElement with "PanSettings" as the name for FromXml
                    XElement rearPanXml = new XElement("PanSettings", rearPanSettingsElement.Elements());
                    RearPan.FromXml(rearPanXml);
                }

                // Parse FrontBladeSettings
                XElement? frontBladeSettingsElement = root.Element("FrontBladeSettings");
                if (frontBladeSettingsElement != null)
                {
                    // Create a wrapper XElement with "BladeConfiguration" as the name for FromXml
                    XElement frontBladeXml = new XElement("BladeConfiguration", frontBladeSettingsElement.Elements());
                    FrontBlade.FromXml(frontBladeXml);
                }

                // Parse RearBladeSettings
                XElement? rearBladeSettingsElement = root.Element("RearBladeSettings");
                if (rearBladeSettingsElement != null)
                {
                    // Create a wrapper XElement with "BladeConfiguration" as the name for FromXml
                    XElement rearBladeXml = new XElement("BladeConfiguration", rearBladeSettingsElement.Elements());
                    RearBlade.FromXml(rearBladeXml);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading equipment settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
