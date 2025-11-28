using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HardwareSim
{
    public enum PGNValues
    {
        // misc
        PGN_ESTOP = 0x0000,
        PGN_RESET = 0x0001,
        PGN_AGGRADE_STARTED = 0x0002,
        PGN_PING = 0x0003,
        PGN_CLEAR_ESTOP = 0x0004,
        PGN_TRACTOR_IMU_FOUND = 0x0005,
        PGN_TRACTOR_IMU_LOST = 0x0006,
        PGN_FRONT_IMU_FOUND = 0x0007,
        PGN_FRONT_IMU_LOST = 0x0008,
        PGN_REAR_IMU_FOUND = 0x0009,
        PGN_REAR_IMU_LOST = 0x000A,
        PGN_FRONT_HEIGHT_FOUND = 0x000B,
        PGN_FRONT_HEIGHT_LOST = 0x000C,
        PGN_REAR_HEIGHT_FOUND = 0x000D,
        PGN_REAR_HEIGHT_LOST = 0x000E,

        // blade control
        PGN_FRONT_CUT_VALVE = 0x1000,   // CUTVALVE_MIN -> CUTVALVE_MAX
        PGN_REAR_CUT_VALVE = 0x1001,   // CUTVALVE_MIN -> CUTVALVE_MAX
        PGN_FRONT_ZERO_BLADE_HEIGHT = 0x1002,
        PGN_REAR_ZERO_BLADE_HEIGHT = 0x1003,

        // blade configuration
        PGN_FRONT_PWM_GAIN_UP = 0x2002,
        PGN_FRONT_PWM_GAIN_DOWN = 0x2003,
        PGN_FRONT_PWM_MIN_UP = 0x2004,
        PGN_FRONT_PWM_MIN_DOWN = 0x2005,
        PGN_FRONT_PWM_MAX_UP = 0x2006,
        PGN_FRONT_PWM_MAX_DOWN = 0x2007,
        PGN_FRONT_INTEGRAL_MULTPLIER = 0x2008,
        PGN_FRONT_DEADBAND = 0x2009,
        PGN_REAR_PWM_GAIN_UP = 0x200A,
        PGN_REAR_PWM_GAIN_DOWN = 0x200B,
        PGN_REAR_PWM_MIN_UP = 0x200C,
        PGN_REAR_PWM_MIN_DOWN = 0x200D,
        PGN_REAR_PWM_MAX_UP = 0x200E,
        PGN_REAR_PWM_MAX_DOWN = 0x200F,
        PGN_REAR_INTEGRAL_MULTPLIER = 0x2010,
        PGN_REAR_DEADBAND = 0x2011,

        // autosteer control
        PGN_AUTOSTEER_RELAY = 0x3000,
        PGN_AUTOSTEER_SPEED = 0x3001,
        PGN_AUTOSTEER_DISTANCE = 0x3002,
        PGN_AUTOSTEER_ANGLE = 0x3003,

        // autosteer configuration
        PGN_AUTOSTEER_KP = 0x4000,
        PGN_AUTOSTEER_KI = 0x4001,
        PGN_AUTOSTEER_KD = 0x4002,
        PGN_AUTOSTEER_KO = 0x4003,
        PGN_AUTOSTEER_OFFSET = 0x4004,
        PGN_AUTOSTEER_MIN_PWM = 0x4005,
        PGN_AUTOSTEER_MAX_INTEGRAL = 0x4006,
        PGN_AUTOSTEER_COUNTS_PER_DEG = 0x4007,

        // blade status
        PGN_FRONT_BLADE_OFFSET_SLAVE = 0x5000,
        PGN_FRONT_BLADE_PWMVALUE = 0x5001,
        PGN_FRONT_BLADE_DIRECTION = 0x5002,
        PGN_FRONT_BLADE_AUTO = 0x5003,
        PGN_REAR_BLADE_OFFSET_SLAVE = 0x5004,
        PGN_REAR_BLADE_PWMVALUE = 0x5005,
        PGN_REAR_BLADE_DIRECTION = 0x5006,
        PGN_REAR_BLADE_AUTO = 0x5007,
        PGN_FRONT_BLADE_HEIGHT = 0x5008,
        PGN_REAR_BLADE_HEIGHT = 0x5009,

        // IMU
        PGN_TRACTOR_PITCH = 0x6000,
        PGN_TRACTOR_ROLL = 0x6001,
        PGN_TRACTOR_HEADING = 0x6002,
        PGN_TRACTOR_YAWRATE = 0x6003,
        PGN_TRACTOR_IMUCALIBRATION = 0x6004,
        PGN_FRONT_PITCH = 0x6005,
        PGN_FRONT_ROLL = 0x6006,
        PGN_FRONT_HEADING = 0x6007,
        PGN_FRONT_YAWRATE = 0x6008,
        PGN_FRONT_IMUCALIBRATION = 0x6009,
        PGN_REAR_PITCH = 0x600A,
        PGN_REAR_ROLL = 0x600B,
        PGN_REAR_HEADING = 0x600C,
        PGN_REAR_YAWRATE = 0x600D,
        PGN_REAR_IMUCALIBRATION = 0x600E,

        // GNSS
        PGN_TRACTOR_NMEA = 0x7000,
    }

    public class PGNPacket
    {
        public const int MAX_LEN = 82;

        public PGNValues PGN;
        public byte[] Data = new byte[MAX_LEN];

        public PGNPacket
            (
            PGNValues PGN,
            byte[] Data
            )
        {
            this.PGN = PGN;

            for (int b = 0; b < MAX_LEN; b++) this.Data[b] = 0;

            int Len = Data.Length;
            if (Len > MAX_LEN) Len = MAX_LEN;

            for (int b = 0; b < Len; b++)
            {
                this.Data[b] = Data[b];
            }
        }

        public PGNPacket
            (
            PGNValues PGN,
            UInt32 Value
            ) : this(PGN, new byte[] { (byte)(Value & 0xFF), (byte)((Value >> 8) & 0xFF), (byte)((Value >> 16) & 0xFF), (byte)((Value >> 24) & 0xFF) })
        {
        }

        public PGNPacket
            (
            PGNValues PGN,
            UInt64 Value
            ) : this(PGN, new byte[]
            {
                (byte)(Value & 0xFF), (byte)((Value >> 8) & 0xFF), (byte)((Value >> 16) & 0xFF), (byte)((Value >> 24) & 0xFF),
                (byte)((Value >> 32) & 0xFF), (byte)((Value >> 40) & 0xFF), (byte)((Value >> 48) & 0xFF), (byte)((Value >> 56) & 0xFF)
            })
        {
        }

        public PGNPacket
            (
            PGNValues PGN,
            UInt64 Value1,
            UInt64 Value2
            ) : this(PGN, new byte[]
            {
                (byte)(Value1 & 0xFF), (byte)((Value1 >> 8) & 0xFF), (byte)((Value1 >> 16) & 0xFF), (byte)((Value1 >> 24) & 0xFF),
                (byte)((Value1 >> 32) & 0xFF), (byte)((Value1 >> 40) & 0xFF), (byte)((Value1 >> 48) & 0xFF), (byte)((Value1 >> 56) & 0xFF),
                (byte)(Value2 & 0xFF), (byte)((Value2 >> 8) & 0xFF), (byte)((Value2 >> 16) & 0xFF), (byte)((Value2 >> 24) & 0xFF),
                (byte)((Value2 >> 32) & 0xFF), (byte)((Value2 >> 40) & 0xFF), (byte)((Value2 >> 48) & 0xFF), (byte)((Value2 >> 56) & 0xFF),
            })
        {
        }

        public PGNPacket
            (
            PGNValues PGN
            ) : this(PGN, 0)
        {
        }

        public PGNPacket
            (
            ) : this(0)
        {
        }

        /// <summary>
        /// Gets a UInt32 from the packet data
        /// </summary>
        /// <returns>UInt32 value</returns>
        public UInt32 GetUInt32
            (
            )
        {
            return ((UInt32)Data[3] << 24) | ((UInt32)Data[2] << 16) | ((UInt32)Data[1] << 8) | Data[0];
        }

        public UInt64 GetUInt64
            (
            )
        {
            return ((UInt64)Data[7] << 56) | ((UInt64)Data[6] << 48) | ((UInt64)Data[5] << 40) | ((UInt64)Data[4] << 32) |
                   ((UInt64)Data[3] << 24) | ((UInt64)Data[2] << 16) | ((UInt64)Data[1] << 8) | Data[0];
        }

        public UInt64[] GetUInt64Array
            (
            )
        {
            List<UInt64> Values = new List<UInt64>();

            Values.Add((((UInt64)Data[7]) << 56) | (((UInt64)Data[6]) << 48) | (((UInt64)Data[5]) << 40) | (((UInt64)Data[4]) << 32) |
                       (((UInt64)Data[3]) << 24) | (((UInt64)Data[2]) << 16) | (((UInt64)Data[1]) << 8) | Data[0]);

            Values.Add((((UInt64)Data[15]) << 56) | (((UInt64)Data[14]) << 48) | (((UInt64)Data[13]) << 40) | (((UInt64)Data[12]) << 32) |
                       (((UInt64)Data[11]) << 24) | (((UInt64)Data[10]) << 16) | (((UInt64)Data[9]) << 8) | Data[8]);

            return Values.ToArray();
        }

        public byte GetByte
            (
            )
        {
            return Data[0];
        }
    }
}
