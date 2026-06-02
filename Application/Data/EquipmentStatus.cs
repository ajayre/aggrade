using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AgGrade.Controller;

namespace AgGrade.Data
{
    public class PanStatus
    {
        public enum BladeDirection
        {
            None,
            Up,
            Down
        }

        /// <summary>
        /// These values are not arbitrary. If they are changed, then change the controller
        /// firmware to match!
        /// </summary>
        public enum BladeMode
        {
            Raised      = 0,
            Manual      = 1,
            AutoCutting = 2,
            AutoFilling = 3,
            Floating    = 4
        }

        public IMUValue IMU;
        public IMUValue ApronIMU;
        public IMUValue BucketIMU;
        public GNSSFix Fix;
        public int BladeHeight;
        public int BladeOffset;
        public BladeMode Mode;
        public byte BladePWM;
        public BladeDirection Direction;
        public bool CapacityWarningOccurred;
        public double LoadLCY;
        public double ApronAngle;
        public double BucketAngle;

        public PanStatus
            (
            )
        {
            IMU = new IMUValue();
            ApronIMU = new IMUValue();
            BucketIMU = new IMUValue();
            Fix = new GNSSFix();

            CapacityWarningOccurred = false;
            Mode = BladeMode.Manual;
            LoadLCY = 0;
            ApronAngle = 0;
            BucketAngle = 0;
        }
    }

    public class EquipmentStatus
    {
        public IMUValue TractorIMU;
        public GNSSFix TractorFix;
        public PanStatus FrontPan;
        public PanStatus RearPan;
        public GnssQualityState TractorFixQuality;
        public GnssQualityState FrontPanFixQuality;
        public GnssQualityState RearPanFixQuality;

        public EquipmentStatus
            (
            )
        {
            TractorIMU = new IMUValue();
            TractorFix = new GNSSFix();
            FrontPan = new PanStatus();
            RearPan = new PanStatus();

            TractorFixQuality = GnssQualityState.NoData;
            FrontPanFixQuality = GnssQualityState.NoData;
            RearPanFixQuality = GnssQualityState.NoData;
        }
    }
}
