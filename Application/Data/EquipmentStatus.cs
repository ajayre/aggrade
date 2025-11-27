using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgGrade.Data
{
    public enum RTKStatus
    {
        None,
        Float,
        Fix
    }

    public class PanStatus
    {
        public double Longitude;
        public double Latitude;
        public double Pitch;
        public double Roll;
        public double Heading;
        public double YawRate;
        public RTKStatus RTK;
        public double BladeHeight;
        public uint IMUCalibrationStatus;

        public PanStatus
            (
            )
        {
            RTK = RTKStatus.None;
        }
    }

    public class EquipmentStatus
    {
        public double TractorLongitude;
        public double TractorLatitude;
        public double TractorPitch;
        public double TractorRoll;
        public double TractorHeading;
        public double TractorYawRate;
        public RTKStatus TractorRTK;
        public uint TractorIMUCalibrationStatus;
        public PanStatus FrontPan;
        public PanStatus RearPan;

        public EquipmentStatus
            (
            )
        {
            TractorRTK = RTKStatus.None;

            FrontPan = new PanStatus();
            RearPan = new PanStatus();
        }
    }
}
