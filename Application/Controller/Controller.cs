using AgGrade.Data;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Text;
using static System.Windows.Forms.AxHost;

namespace AgGrade.Controller
{
    public enum EquipType
    {
        Tractor,
        Front,
        Rear
    }

    public class OGController
    {
        public const uint CUTVALVE_MIN = 0;
        public const uint CUTVALVE_MAX = 200;

        private const Int64 LOCATION_SCALE_FACTOR = 1000000000;

        public event Action OnControllerLost = null;
        public event Action OnControllerFound = null;

        public event Action<EquipType> OnIMUFound = null;
        public event Action<EquipType> OnIMULost = null;

        public event Action<EquipType> OnHeightFound = null;
        public event Action<EquipType> OnHeightLost = null;

        public delegate void EmergencyStop();
        public event EmergencyStop OnEmergencyStop = null;

        public delegate void EmergencyStopCleared();
        public event EmergencyStopCleared OnEmergencyStopCleared = null;

        public delegate void SlaveOffsetChanged(int Offset);
        public event SlaveOffsetChanged OnFrontSlaveOffsetChanged = null;
        public event SlaveOffsetChanged OnRearSlaveOffsetChanged = null;

        public delegate void BladeCuttingChanged(bool IsCutting);
        public event BladeCuttingChanged OnFrontBladeCuttingChanged = null;
        public event BladeCuttingChanged OnRearBladeCuttingChanged = null;

        public delegate void DumpingChanged(bool IsDumping);
        public event DumpingChanged OnFrontDumpingChanged = null;
        public event DumpingChanged OnRearDumpingChanged = null;

        public delegate void BladeDirectionChanged(bool IsMovingUp);
        public event BladeDirectionChanged OnFrontBladeDirectionChanged = null;
        public event BladeDirectionChanged OnRearBladeDirectionChanged = null;

        public delegate void BladePWMChanged(byte PWMValue);
        public event BladePWMChanged OnFrontBladePWMChanged = null;
        public event BladePWMChanged OnRearBladePWMChanged = null;

        public delegate void IMUChanged(IMUValue Value);
        public event IMUChanged OnTractorIMUChanged = null;
        public event IMUChanged OnFrontIMUChanged = null;
        public event IMUChanged OnRearIMUChanged = null;

        public delegate void LocationChanged(GNSSFix Fix);
        public event LocationChanged OnTractorLocationChanged = null;
        public event LocationChanged OnFrontLocationChanged = null;
        public event LocationChanged OnRearLocationChanged = null;

        public delegate void BladeHeightChanged(uint Height);
        public event BladeHeightChanged OnFrontBladeHeightChanged = null;
        public event BladeHeightChanged OnRearBladeHeightChanged = null;

        public delegate void BladeCommandSent(uint Value);
        public event BladeCommandSent OnFrontBladeCommandSent = null;
        public event BladeCommandSent OnRearBladeCommandSent = null;

        // time between transmit of pings in milliseconds
        private const int PING_PERIOD_MS = 1000;

        // maximum time to wait for a ping before determining controller has stopped working
        private const int PING_TIMEOUT_PERIOD_MS = 3000;

        private UDPTransfer ControllerChannel;
        private IMUValue TractorIMU = new IMUValue();
        private IMUValue FrontScraperIMU = new IMUValue();
        private IMUValue RearScraperIMU = new IMUValue();

        private DateTime LastRxPingTime;
        private Thread WorkThread = null;
        private volatile bool WorkThreadCancellationRequested = false;
        private DateTime PingTime;
        private GNSSVector? TractorVector = null;
        private GNSSFix? TractorFix = null;
        private GNSSVector? FrontVector = null;
        private GNSSFix? FrontFix = null;
        private GNSSVector? RearVector = null;
        private GNSSFix? RearFix = null;
        private SensorFusor Fusor = new SensorFusor();
        private EquipmentSettings CurrentEquipmentSettings = new EquipmentSettings();

        private bool _IsControllerFound;
        public bool IsControllerFound { get { return _IsControllerFound; } }

        /// <summary>
        /// Connect to controller
        /// </summary>
        /// <param name="Address">IP address of controller</param>
        /// <param name="SubnetMask">IP Subnet mask</param>
        /// <param name="RemotePort">Port number that controller is listening on</param>
        /// <param name="LocalPort">Port number that AgGrade will listen on</param>
        public void Connect
            (
            IPAddress Address,
            int RemotePort,
            IPAddress SubnetMask,
            int LocalPort
            )
        {
            // Close any existing connection before creating a new one
            if (ControllerChannel != null)
            {
                ControllerChannel.Close();
            }
            
            ControllerChannel = new UDPTransfer();
            ControllerChannel.Begin(Address, RemotePort, SubnetMask, LocalPort);

            // tell controller we are now running
            SendControllerCommand(new PGNPacket(PGNValues.PGN_AGGRADE_STARTED));

            PingTime = DateTime.Now.AddMilliseconds(PING_PERIOD_MS);

            LastRxPingTime = DateTime.Now;
            _IsControllerFound = false;

            WorkThreadCancellationRequested = false;
            WorkThread = new Thread(WorkThread_DoWork);
            WorkThread.Start();
        }

        public void Disconnect
            (
            )
        {
            WorkThreadCancellationRequested = true;

            if (WorkThread != null)
            {
                if (WorkThread.IsAlive)
                {
                    WorkThread.Join(1000); // Wait up to 1 second for thread to finish
                }
                WorkThread = null;
            }
            
            // Close the UDP connection
            if (ControllerChannel != null)
            {
                ControllerChannel.Close();
                ControllerChannel = null;
            }
        }

        /// <summary>
        /// Sets the equipment settings
        /// </summary>
        /// <param name="Settings">New settings</param>
        public void SetEquipmentSettings
            (
            EquipmentSettings Settings
            )
        {
            CurrentEquipmentSettings = Settings;
        }

        /// <summary>
        /// Sets the front blade configuration
        /// </summary>
        /// <param name="Config">New configuration</param>
        public void SetFrontBladeConfiguration
            (
            BladeConfiguration Config
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_PWM_GAIN_UP,        Config.PWMGainUp));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_PWM_GAIN_DOWN,      Config.PWMGainDown));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_PWM_MIN_UP,         Config.PWMMinUp));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_PWM_MIN_DOWN,       Config.PWMMinDown));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_PWM_MAX_UP,         Config.PWMMaxUp));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_PWM_MAX_DOWN,       Config.PWMMaxDown));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_INTEGRAL_MULTPLIER, Config.IntegralMultiplier));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_DEADBAND,           Config.Deadband));
        }

        /// <summary>
        /// Sets the rear blade configuration
        /// </summary>
        /// <param name="Config">New configuration</param>
        public void SetRearBladeConfiguration
            (
            BladeConfiguration Config
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_PWM_GAIN_UP,        Config.PWMGainUp));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_PWM_GAIN_DOWN,      Config.PWMGainDown));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_PWM_MIN_UP,         Config.PWMMinUp));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_PWM_MIN_DOWN,       Config.PWMMinDown));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_PWM_MAX_UP,         Config.PWMMaxUp));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_PWM_MAX_DOWN,       Config.PWMMaxDown));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_INTEGRAL_MULTPLIER, Config.IntegralMultiplier));
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_DEADBAND,           Config.Deadband));
        }

        /// <summary>
        /// Sets the current front blade position to zero above ground
        /// Send this after lowering the blade to the ground using GNSS height
        /// </summary>
        public void FrontBladeAtZero
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_ZERO_BLADE_HEIGHT));
        }

        /// <summary>
        /// Sets the current rear blade position to zero above ground
        /// Send this after lowering the blade to the ground using GNSS height
        /// </summary>
        public void RearBladeAtZero
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_ZERO_BLADE_HEIGHT));
        }

        /// <summary>
        /// Communicates with the controller
        /// </summary>
        private void WorkThread_DoWork()
        {
            while (!WorkThreadCancellationRequested)
            {
                // send ping to controller to tell it we are alive
                if (PingTime < DateTime.Now)
                {
                    PingTime = DateTime.Now.AddMilliseconds(PING_PERIOD_MS);

                    SendControllerCommand(new PGNPacket(PGNValues.PGN_PING));
                }

                // message waiting from controller
                if (ControllerChannel.Available() > 0)
                {
                    PGNPacket Stat = GetControllerStatus();
                    switch (Stat.PGN)
                    {
                        // misc
                        case PGNValues.PGN_ESTOP:
                            OnEmergencyStop?.Invoke();
                            break;

                        case PGNValues.PGN_CLEAR_ESTOP:
                            OnEmergencyStopCleared?.Invoke();
                            break;

                        case PGNValues.PGN_PING:
                            LastRxPingTime = DateTime.Now;
                            if (!_IsControllerFound)
                            {
                                _IsControllerFound = true;
                                OnControllerFound?.Invoke();
                            }
                            break;

                        case PGNValues.PGN_TRACTOR_IMU_FOUND:
                            OnIMUFound?.Invoke(EquipType.Tractor);
                            break;

                        case PGNValues.PGN_TRACTOR_IMU_LOST:
                            OnIMULost?.Invoke(EquipType.Tractor);
                            break;

                        case PGNValues.PGN_FRONT_IMU_FOUND:
                            OnIMUFound?.Invoke(EquipType.Front);
                            break;

                        case PGNValues.PGN_FRONT_IMU_LOST:
                            OnIMULost?.Invoke(EquipType.Front);
                            break;

                        case PGNValues.PGN_REAR_IMU_FOUND:
                            OnIMUFound?.Invoke(EquipType.Rear);
                            break;

                        case PGNValues.PGN_REAR_IMU_LOST:
                            OnIMULost?.Invoke(EquipType.Rear);
                            break;

                        case PGNValues.PGN_FRONT_HEIGHT_FOUND:
                            OnHeightFound?.Invoke(EquipType.Front);
                            break;

                        case PGNValues.PGN_FRONT_HEIGHT_LOST:
                            OnHeightLost?.Invoke(EquipType.Front);
                            break;

                        case PGNValues.PGN_REAR_HEIGHT_FOUND:
                            OnHeightFound?.Invoke(EquipType.Rear);
                            break;

                        case PGNValues.PGN_REAR_HEIGHT_LOST:
                            OnHeightLost?.Invoke(EquipType.Rear);
                            break;

                        // slave offsets
                        case PGNValues.PGN_FRONT_BLADE_OFFSET_SLAVE:
                            OnFrontSlaveOffsetChanged?.Invoke((int)Stat.GetUInt32());
                            break;

                        case PGNValues.PGN_REAR_BLADE_OFFSET_SLAVE:
                            OnRearSlaveOffsetChanged?.Invoke((int)Stat.GetUInt32());
                            break;

                        // blade heights
                        case PGNValues.PGN_FRONT_BLADE_HEIGHT:
                            OnFrontBladeHeightChanged?.Invoke(Stat.GetUInt32());
                            break;

                        case PGNValues.PGN_REAR_BLADE_HEIGHT:
                            OnRearBladeHeightChanged?.Invoke(Stat.GetUInt32());
                            break;

                        // IMU
                        case PGNValues.PGN_TRACTOR_IMU:
                            TractorIMU.Pitch = ((Int32)Stat.GetUInt32(0)) / 100.0;
                            TractorIMU.Roll = ((Int32)Stat.GetUInt32(4)) / 100.0;
                            TractorIMU.Heading = ((Int32)Stat.GetUInt32(8)) / 100.0;
                            TractorIMU.YawRate = ((Int32)Stat.GetUInt32(12)) / 100.0;
                            TractorIMU.CalibrationStatus = (IMUValue.Calibration)Stat.GetByte(16);
                            OnTractorIMUChanged?.Invoke(TractorIMU);
                            break;

                        case PGNValues.PGN_FRONT_IMU:
                            FrontScraperIMU.Pitch = ((Int32)Stat.GetUInt32(0)) / 100.0;
                            FrontScraperIMU.Roll = ((Int32)Stat.GetUInt32(4)) / 100.0;
                            FrontScraperIMU.Heading = ((Int32)Stat.GetUInt32(8)) / 100.0;
                            FrontScraperIMU.YawRate = ((Int32)Stat.GetUInt32(12)) / 100.0;
                            FrontScraperIMU.CalibrationStatus = (IMUValue.Calibration)Stat.GetByte(16);
                            OnFrontIMUChanged?.Invoke(FrontScraperIMU);
                            break;

                        case PGNValues.PGN_REAR_IMU:
                            RearScraperIMU.Pitch = ((Int32)Stat.GetUInt32(0)) / 100.0;
                            RearScraperIMU.Roll = ((Int32)Stat.GetUInt32(4)) / 100.0;
                            RearScraperIMU.Heading = ((Int32)Stat.GetUInt32(8)) / 100.0;
                            RearScraperIMU.YawRate = ((Int32)Stat.GetUInt32(12)) / 100.0;
                            RearScraperIMU.CalibrationStatus = (IMUValue.Calibration)Stat.GetByte(16);
                            OnRearIMUChanged?.Invoke(RearScraperIMU);
                            break;

                        // scraper dumping flags
                        case PGNValues.PGN_FRONT_DUMPING:
                            bool Dumping = Stat.GetByte() == 1 ? true : false;
                            OnFrontDumpingChanged?.Invoke(Dumping);
                            break;

                        case PGNValues.PGN_REAR_DUMPING:
                            Dumping = Stat.GetByte() == 1 ? true : false;
                            OnRearDumpingChanged?.Invoke(Dumping);
                            break;

                        // blade auto flags
                        case PGNValues.PGN_FRONT_CUTTING:
                            bool Cutting = Stat.GetByte() == 1 ? true : false;
                            OnFrontBladeCuttingChanged?.Invoke(Cutting);
                            break;

                        case PGNValues.PGN_REAR_CUTTING:
                            Cutting = Stat.GetByte() == 1 ? true : false;
                            OnRearBladeCuttingChanged?.Invoke(Cutting);
                            break;

                        // blade direction
                        case PGNValues.PGN_FRONT_BLADE_DIRECTION:
                            bool Up = Stat.GetByte() == 1 ? true : false;
                            OnFrontBladeDirectionChanged?.Invoke(Up);
                            break;

                        case PGNValues.PGN_REAR_BLADE_DIRECTION:
                            Up = Stat.GetByte() == 1 ? true : false;
                            OnRearBladeDirectionChanged?.Invoke(Up);
                            break;

                        case PGNValues.PGN_FRONT_BLADE_PWMVALUE:
                            OnFrontBladePWMChanged?.Invoke(Stat.Data[0]);
                            break;

                        case PGNValues.PGN_REAR_BLADE_PWMVALUE:
                            OnRearBladePWMChanged?.Invoke(Stat.Data[0]);
                            break;

                        // location
                        case PGNValues.PGN_TRACTOR_NMEA:
                            {
                                string Sentence = Encoding.ASCII.GetString(Stat.Data);
                                ProcessNMEA(Sentence, ref TractorFix, ref TractorVector, OnTractorLocationChanged,
                                    TractorIMU, CurrentEquipmentSettings.TractorAntennaHeightMm, CurrentEquipmentSettings.TractorAntennaLeftOffsetMm,
                                    CurrentEquipmentSettings.TractorAntennaForwardOffsetMm);
                            }
                            break;

                        case PGNValues.PGN_FRONT_NMEA:
                            {
                                string Sentence = Encoding.ASCII.GetString(Stat.Data);
                                ProcessNMEA(Sentence, ref FrontFix, ref FrontVector, OnFrontLocationChanged,
                                    FrontScraperIMU, CurrentEquipmentSettings.FrontPan.AntennaHeightMm, 0, 0);
                            }
                            break;
                        
                        case PGNValues.PGN_REAR_NMEA:
                            {
                                string Sentence = Encoding.ASCII.GetString(Stat.Data);
                                ProcessNMEA(Sentence, ref RearFix, ref RearVector, OnRearLocationChanged,
                                    RearScraperIMU, CurrentEquipmentSettings.RearPan.AntennaHeightMm, 0, 0);
                            }
                            break;
                    }
                }

                // controller has disappeared (check after processing messages to avoid race condition)
                if ((DateTime.Now >= LastRxPingTime.AddMilliseconds(PING_TIMEOUT_PERIOD_MS)) && _IsControllerFound)
                {
                    _IsControllerFound = false;

                    OnControllerLost?.Invoke();
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Process an NMEA sentence
        /// </summary>
        /// <param name="Sentence">Sentence to process</param>
        /// <param name="Fix">Equipment fix to update</param>
        /// <param name="Vector">Equipment vector to update</param>
        /// <param name="LocChangedEvent">Event to raise</param>
        /// <param name="IMU">The latest IMU reading</param>
        /// <param name="AntennaHeightMm">Height of antenna in millimeters</param>
        /// <param name="AntennaLeftOffsetMm">Left offset of antenna in millimeters</param>
        /// <param name="AntennaForwardOffsetMm">Forward offset of antenna in millimeters</param>
        private void ProcessNMEA
            (
            string Sentence,
            ref GNSSFix? Fix,
            ref GNSSVector? Vector,
            LocationChanged LocChangedEvent,
            IMUValue IMU,
            uint AntennaHeightMm,
            int AntennaLeftOffsetMm,
            int AntennaForwardOffsetMm
            )
        {
            if (Sentence.StartsWith("$GNGGA"))
            {
                try
                {
                    Fix = GNSSFix.ParseNMEA(Sentence);
                    if (Vector != null)
                    {
                        Fix.Vector = Vector.Clone();
                    }

                    // fuse with IMU
                    if ((TractorIMU.CalibrationStatus == IMUValue.Calibration.Good) || (TractorIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    {
                        Fix = Fusor.Fuse(Fix, IMU, AntennaHeightMm, AntennaLeftOffsetMm, AntennaForwardOffsetMm);
                    }

                    LocChangedEvent?.Invoke(Fix);
                }
                catch (NMEAParseException)
                {
                    // throw away
                }
            }
            else if (Sentence.StartsWith("$GPVTG") || Sentence.StartsWith("$GNVTG"))
            {
                try
                {
                    Vector = GNSSVector.ParseNMEA(Sentence);
                    if (Fix != null)
                    {
                        Fix.Vector = Vector.Clone();

                        // fuse with IMU
                        if ((TractorIMU.CalibrationStatus == IMUValue.Calibration.Good) || (TractorIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                        {
                            Fix = Fusor.Fuse(Fix, IMU, AntennaHeightMm, AntennaLeftOffsetMm, AntennaForwardOffsetMm);
                        }

                        LocChangedEvent?.Invoke(Fix);
                    }
                }
                catch (NMEAParseException)
                {
                    // throw away
                }
            }
        }

        /// <summary>
        /// Tell the controller we have changed the blade mode
        /// </summary>
        /// <param name="Mode">New blade mode</param>
        public void SetFrontBladeMode
            (
            PanStatus.BladeMode Mode
            )
        {
            byte State = 0;

            if (Mode == PanStatus.BladeMode.AutoCutting) State = 1;

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_FRONT_CUTTING, new byte[] { State });
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Tell the controller we have changed the blade mode
        /// </summary>
        /// <param name="Mode">New blade mode</param>
        public void SetRearBladeMode
            (
            PanStatus.BladeMode Mode
            )
        {
            byte State = 0;

            if (Mode == PanStatus.BladeMode.AutoCutting) State = 1;

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_REAR_CUTTING, new byte[] { State });
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sets the front cut valve
        /// </summary>
        /// <param name="Value">CUTVALVE_MIN -> CUTVALVE_MAX with 100 = at target height</param>
        public void SetFrontCutValve
            (
            uint Value
            )
        {
            if (Value > CUTVALVE_MAX) Value = CUTVALVE_MAX;

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_FRONT_CUT_VALVE, Value);
            SendControllerCommand(TxCmd);

            OnFrontBladeCommandSent?.Invoke(Value);
        }

        /// <summary>
        /// Sets the rear cut valve
        /// </summary>
        /// <param name="Value">CUTVALVE_MIN -> CUTVALVE_MAX with 100 = at target height</param>
        public void SetRearCutValve
            (
            uint Value
            )
        {
            if (Value > CUTVALVE_MAX) Value = CUTVALVE_MAX;

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_REAR_CUT_VALVE, Value);
            SendControllerCommand(TxCmd);

            OnRearBladeCommandSent?.Invoke(Value);
        }

        /// <summary>
        /// Resets the controller which in turn will reset all nodes on the network
        /// WARNING: this will cause the COM port to disappear and then return
        /// </summary>
        public void ResetController
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_RESET));
        }

        private void SendControllerCommand
            (
            PGNPacket Cmd
            )
        {
            if (ControllerChannel != null)
            {
                ControllerChannel.Packet.TxBuff[0] = (byte)((UInt16)Cmd.PGN & 0xFF);
                ControllerChannel.Packet.TxBuff[1] = (byte)(((UInt16)Cmd.PGN >> 8) & 0xFF);

                for (int b = 0; b < PGNPacket.MAX_LEN; b++)
                {
                    ControllerChannel.Packet.TxBuff[2 + b] = Cmd.Data[b];
                }

                ControllerChannel.SendData(PGNPacket.MAX_LEN + 2);
                //Console.WriteLine(string.Format("Tx: {0}:0x{1:X8}", Cmd.PGN, Cmd.Value));
            }
        }

        private PGNPacket GetControllerStatus
            (
            )
        {
            PGNPacket Stat = new PGNPacket();

            Stat.PGN = (PGNValues)(((UInt16)ControllerChannel.Packet.RxBuff[1] << 8) | ControllerChannel.Packet.RxBuff[0]);

            for (int b = 0; b < PGNPacket.MAX_LEN; b++)
            {
                Stat.Data[b] = ControllerChannel.Packet.RxBuff[2 + b];
            }

            //Console.WriteLine(string.Format("Rx: {0}:0x{1:X8}", Stat.PGN, Stat.Value));

            return Stat;
        }
    }
}
