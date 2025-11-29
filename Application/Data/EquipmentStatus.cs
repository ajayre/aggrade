using System;
using System.Collections.Generic;
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

        public IMUValue IMU; 
        public GNSSFix Fix;
        public int BladeHeight;
        public int BladeOffset;
        public bool BladeAuto;
        public byte BladePWM;
        public BladeDirection Direction;

        public PanStatus
            (
            )
        {
            IMU = new IMUValue();
            Fix = new GNSSFix();
        }
    }

    public class EquipmentStatus
    {
        public IMUValue TractorIMU;
        public GNSSFix TractorFix;
        public PanStatus FrontPan;
        public PanStatus RearPan;

        public EquipmentStatus
            (
            )
        {
            TractorIMU = new IMUValue();
            TractorFix = new GNSSFix();
            FrontPan = new PanStatus();
            RearPan = new PanStatus();
        }
    }
}
