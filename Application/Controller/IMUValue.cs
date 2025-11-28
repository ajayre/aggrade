using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller
{
    public class IMUValue
    {
        public double Pitch;
        public double Heading;
        public double Roll;
        public double YawRate;           // deg/s
        public byte CalibrationStatus;   // 0 - 4, 4 = best

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
