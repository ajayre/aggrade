using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controller;

namespace AgGrade.Data
{
    public class PanStatus
    {
        public double Pitch;
        public double Roll;
        public double Heading;
        public double YawRate;
        public GNSSFix Fix;
        public double BladeHeight;
        public uint IMUCalibrationStatus;

        public PanStatus
            (
            )
        {
            Fix = new GNSSFix();
        }
    }

    public class EquipmentStatus
    {
        public double TractorPitch;
        public double TractorRoll;
        public double TractorHeading;
        public double TractorYawRate;
        public GNSSFix TractorFix;
        public uint TractorIMUCalibrationStatus;
        public PanStatus FrontPan;
        public PanStatus RearPan;

        public EquipmentStatus
            (
            )
        {
            TractorFix = new GNSSFix();
            FrontPan = new PanStatus();
            RearPan = new PanStatus();
        }
    }
}
