using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareSim
{
    public class IMUValue
    {
        public enum Calibration
        {
            None = 0,
            Poor = 1,
            Adequate = 2,
            Good = 3,
            Excellent = 4
        }

        public double Pitch;
        public double Heading;
        public double Roll;
        public double YawRate;                 // deg/s
        public Calibration CalibrationStatus;

        public IMUValue
            (
            )
        {
        }

        /// <summary>
        /// Gets the true heading
        /// </summary>
        /// <param name="MagneticDeclinationDeg">Degrees of magnetic declination</param>
        /// <param name="MagneticDeclinationMin">Minutes of magnetic declination</param>
        /// <returns></returns>
        public double GetTrueHeading
            (
            int MagneticDeclinationDeg,
            uint MagneticDeclinationMin
            )
        {
            int Min = (int)MagneticDeclinationMin;
            if (MagneticDeclinationDeg < 0) Min = -Min;

            double DecDegrees = MagneticDeclinationDeg + (Min / 60.0);
            double TrueHeading = Heading + DecDegrees;

            if (TrueHeading < 0) TrueHeading += 360.0;
            if (TrueHeading >= 360) TrueHeading -= 360.0;

            return TrueHeading;
        }
    }
}
