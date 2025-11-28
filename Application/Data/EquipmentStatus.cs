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
        public IMUValue IMU; 
        public GNSSFix Fix;
        public double BladeHeight;

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
