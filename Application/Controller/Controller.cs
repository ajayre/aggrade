using AgGrade.Data;
using OpenCvSharp;
using System.Collections.Concurrent;
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
        FrontApron,
        FrontBucket,
        Rear,
        RearBucket
    }

    public enum IMUs
    {
        Tractor,
        Front,
        Rear,
        FrontApron,
        FrontBucket,
        RearBucket
    }

    public enum IMUOrientations
    {
        HorizontalA,
        VerticalA
    }

    public class OGController
    {
        public const uint CUTVALVE_MIN = 0;
        public const uint CUTVALVE_MAX = 400;

        private const Int64 LOCATION_SCALE_FACTOR = 1000000000;

        public event Action OnControllerLost = null;
        public event Action OnControllerFound = null;

        public event Action OnEnableSecondaryTabletMode = null;

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
        public event IMUChanged OnFrontApronIMUChanged = null;
        public event IMUChanged OnFrontBucketIMUChanged = null;
        public event IMUChanged OnRearIMUChanged = null;
        public event IMUChanged OnRearBucketIMUChanged = null;

        public delegate void LocationChanged(GNSSFix Fix);
        public event LocationChanged OnTractorLocationChanged = null;
        public event LocationChanged OnFrontLocationChanged = null;
        public event LocationChanged OnRearLocationChanged = null;

        public delegate void BladeHeightChanged(uint Height);
        public event BladeHeightChanged OnFrontBladeHeightChanged = null;
        public event BladeHeightChanged OnRearBladeHeightChanged = null;

        public delegate void AngleChanged(double Angle);
        public event AngleChanged OnFrontApronAngleChanged = null;
        public event AngleChanged OnFrontBucketAngleChanged = null;
        public event AngleChanged OnRearBucketAngleChanged = null;

        public delegate void BladeCommandSent(uint Value);
        public event BladeCommandSent OnFrontBladeCommandSent = null;
        public event BladeCommandSent OnRearBladeCommandSent = null;

        public delegate void BladeJogged(bool Up);
        public event BladeJogged OnFrontBladeJogged = null;
        public event BladeJogged OnRearBladeJogged = null;

        public delegate void GnssStateChanged(GnssQualityState State);
        public event GnssStateChanged OnTractorGnssStateChanged = null;
        public event GnssStateChanged OnFrontGnssStateChanged = null;
        public event GnssStateChanged OnRearGnssStateChanged = null;

        // time between transmit of pings in milliseconds
        private const int PING_PERIOD_MS = 1000;

        // maximum time to wait for a ping before determining controller has stopped working
        private const int PING_TIMEOUT_PERIOD_MS = 3000;

        private UDPTransfer ControllerChannel;
        private IMUValue TractorIMU = new IMUValue();
        private IMUValue FrontScraperIMU = new IMUValue();
        private IMUValue FrontScraperApronIMU = new IMUValue();
        private IMUValue FrontScraperBucketIMU = new IMUValue();
        private IMUValue RearScraperIMU = new IMUValue();
        private IMUValue RearScraperBucketIMU = new IMUValue();

        private DateTime LastRxPingTime;
        private Thread ReceiveThread = null;
        private Thread WorkThread = null;
        private volatile bool WorkThreadCancellationRequested = false;
        private readonly AutoResetEvent WorkSignal = new AutoResetEvent(false);
        private readonly ConcurrentQueue<PGNPacket> UrgentPackets = new ConcurrentQueue<PGNPacket>();
        private readonly object StateLock = new object();
        private bool PendingControllerFound;
        private bool TractorImuDirty;
        private bool FrontImuDirty;
        private bool FrontApronImuDirty;
        private bool FrontBucketImuDirty;
        private bool RearImuDirty;
        private bool RearBucketImuDirty;
        private bool TractorLocationDirty;
        private bool FrontLocationDirty;
        private bool RearLocationDirty;
        private bool FrontBladeHeightDirty;
        private bool RearBladeHeightDirty;
        private bool FrontApronAngleDirty;
        private bool FrontBucketAngleDirty;
        private bool RearBucketAngleDirty;
        private bool FrontSlaveOffsetDirty;
        private bool RearSlaveOffsetDirty;
        private bool FrontBladeDirectionDirty;
        private bool RearBladeDirectionDirty;
        private bool FrontBladePwmDirty;
        private bool RearBladePwmDirty;
        private uint PendingFrontBladeHeight;
        private uint PendingRearBladeHeight;
        private double PendingFrontApronAngle;
        private double PendingFrontBucketAngle;
        private double PendingRearBucketAngle;
        private int PendingFrontSlaveOffset;
        private int PendingRearSlaveOffset;
        private bool PendingFrontBladeDirectionUp;
        private bool PendingRearBladeDirectionUp;
        private byte PendingFrontBladePwm;
        private byte PendingRearBladePwm;
        private DateTime PingTime;
        private GNSSVector? TractorVector = null;
        private GNSSFix? TractorFix = null;
        private GNSSVector? FrontVector = null;
        private GNSSFix? FrontFix = null;
        private GNSSVector? RearVector = null;
        private GNSSFix? RearFix = null;
        private SensorFusor Fusor = new SensorFusor();
        private int MagneticDeclinationDegrees;
        private uint MagneticDeclinationMinutes;
        private EquipmentSettings CurrentEquipmentSettings = new EquipmentSettings();
        private GnssQualityMonitor TractorQualityMonitor = new GnssQualityMonitor();
        private GnssQualityMonitor FrontQualityMonitor = new GnssQualityMonitor();
        private GnssQualityMonitor RearQualityMonitor = new GnssQualityMonitor();

        private bool _IsControllerFound;
        public bool IsControllerFound { get { return _IsControllerFound; } }

        /// <summary>Latest GNSS quality state for the tractor receiver.</summary>
        public GnssQualityState TractorGnssQualityState => TractorQualityMonitor.State;

        /// <summary>Latest GNSS quality state for the front pan receiver.</summary>
        public GnssQualityState FrontGnssQualityState => FrontQualityMonitor.State;

        /// <summary>Latest GNSS quality state for the rear pan receiver.</summary>
        public GnssQualityState RearGnssQualityState => RearQualityMonitor.State;

        /// <summary>
        /// Median tractor position from the quality monitor when state is <see cref="GnssQualityState.HighQuality"/>.
        /// </summary>
        /// <param name="latitude">Median latitude when the method returns true.</param>
        /// <param name="longitude">Median longitude when the method returns true.</param>
        /// <returns>True when a high-quality capture position is available.</returns>
        public bool TryGetTractorHighQualityPosition(out double latitude, out double longitude)
        {
            return TractorQualityMonitor.TryGetHighQualityPosition(out latitude, out longitude);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public OGController
            (
            )
        {
            TractorQualityMonitor.StateChanged += TractorQualityMonitor_StateChanged;
            FrontQualityMonitor.StateChanged += FrontQualityMonitor_StateChanged;
            RearQualityMonitor.StateChanged += RearQualityMonitor_StateChanged;
        }

        /// <summary>
        /// Called when the quality of the rear GNSS fix changes
        /// </summary>
        /// <param name="PreviousState">The state before the change</param>
        /// <param name="NewState">The state after the change</param>
        private void RearQualityMonitor_StateChanged(GnssQualityState PreviousState, GnssQualityState NewState)
        {
            OnRearGnssStateChanged?.Invoke(NewState);
        }

        /// <summary>
        /// Called when the quality of the front GNSS fix changes
        /// </summary>
        /// <param name="PreviousState">The state before the change</param>
        /// <param name="NewState">The state after the change</param>
        private void FrontQualityMonitor_StateChanged(GnssQualityState PreviousState, GnssQualityState NewState)
        {
            OnFrontGnssStateChanged?.Invoke(NewState);
        }

        /// <summary>
        /// Called when the quality of the tractor GNSS fix changes
        /// </summary>
        /// <param name="PreviousState">The state before the change</param>
        /// <param name="NewState">The state after the change</param>
        private void TractorQualityMonitor_StateChanged(GnssQualityState PreviousState, GnssQualityState NewState)
        {
            OnTractorGnssStateChanged?.Invoke(NewState);
        }

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

            ResetQualityMonitors();

            WorkThreadCancellationRequested = false;
            ReceiveThread = new Thread(ReceiveThread_DoWork)
            {
                Name = "UDP Receive",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            WorkThread = new Thread(WorkThread_DoWork)
            {
                Name = "Controller Work",
                IsBackground = true
            };
            ReceiveThread.Start();
            WorkThread.Start();
        }

        public void Disconnect
            (
            )
        {
            WorkThreadCancellationRequested = true;
            WorkSignal.Set();

            if (ReceiveThread != null)
            {
                if (ReceiveThread.IsAlive)
                {
                    ReceiveThread.Join(1000);
                }
                ReceiveThread = null;
            }

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

            ResetQualityMonitors();
        }

        /// <summary>
        /// Clears GNSS quality history so RTK must re-converge after connect or link loss.
        /// </summary>
        private void ResetQualityMonitors()
        {
            TractorQualityMonitor.Reset();
            FrontQualityMonitor.Reset();
            RearQualityMonitor.Reset();
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
        /// Sets the current front apron angle to zero
        /// </summary>
        public void FrontApronAtZero
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_ZERO_APRON_ANGLE));
        }

        /// <summary>
        /// Sets the current front bucket angle to zero
        /// </summary>
        public void FrontBucketAtZero
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_ZERO_BUCKET_ANGLE));
        }

        /// <summary>
        /// Sets the current rear bucket angle to zero
        /// </summary>
        public void RearBucketAtZero
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_ZERO_BUCKET_ANGLE));
        }

        /// <summary>
        /// Request the current front blade height
        /// </summary>
        public void RequestFrontBladeHeight
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_FRONT_BLADE_HEIGHT));
        }

        /// <summary>
        /// Request the current rear blade height
        /// </summary>
        public void RequestRearBladeHeight
            (
            )
        {
            SendControllerCommand(new PGNPacket(PGNValues.PGN_REAR_BLADE_HEIGHT));
        }

        /// <summary>
        /// Drains the UDP socket and applies high-rate updates without raising events
        /// </summary>
        private void ReceiveThread_DoWork()
        {
            while (!WorkThreadCancellationRequested)
            {
                bool receivedData = false;

                while (ControllerChannel.Available() > 0)
                {
                    receivedData = true;
                    PGNPacket stat = GetControllerStatus();

                    lock (StateLock)
                    {
                        ApplyIncomingPacket(stat, out bool urgent);
                        if (urgent)
                        {
                            UrgentPackets.Enqueue(CopyPacket(stat));
                        }
                    }
                }

                if (receivedData)
                {
                    WorkSignal.Set();
                }
                else if (!ControllerChannel.HasPendingInput)
                {
                    Thread.Sleep(0);
                }
            }
        }

        /// <summary>
        /// Sends pings, dispatches coalesced sensor updates, and handles urgent packets
        /// </summary>
        private void WorkThread_DoWork()
        {
            while (!WorkThreadCancellationRequested)
            {
                if (PingTime < DateTime.Now)
                {
                    PingTime = DateTime.Now.AddMilliseconds(PING_PERIOD_MS);
                    SendControllerCommand(new PGNPacket(PGNValues.PGN_PING));
                }

                WorkSignal.WaitOne(1);

                while (UrgentPackets.TryDequeue(out PGNPacket urgentPacket))
                {
                    lock (StateLock)
                    {
                        ProcessUrgentPacket(urgentPacket);
                    }
                }

                lock (StateLock)
                {
                    DispatchCoalescedUpdates();
                }

                if ((DateTime.Now >= LastRxPingTime.AddMilliseconds(PING_TIMEOUT_PERIOD_MS)) && _IsControllerFound)
                {
                    _IsControllerFound = false;
                    ResetQualityMonitors();
                    OnControllerLost?.Invoke();
                }
            }
        }

        private static PGNPacket CopyPacket(PGNPacket source)
        {
            byte[] data = new byte[PGNPacket.MAX_LEN];
            Array.Copy(source.Data, data, PGNPacket.MAX_LEN);
            return new PGNPacket(source.PGN, data);
        }

        private void ApplyIncomingPacket(PGNPacket stat, out bool urgent)
        {
            urgent = false;

            switch (stat.PGN)
            {
                case PGNValues.PGN_PING:
                    LastRxPingTime = DateTime.Now;
                    if (!_IsControllerFound)
                    {
                        PendingControllerFound = true;
                    }
                    break;

                case PGNValues.PGN_ESTOP:
                case PGNValues.PGN_CLEAR_ESTOP:
                case PGNValues.PGN_TRACTOR_IMU_FOUND:
                case PGNValues.PGN_TRACTOR_IMU_LOST:
                case PGNValues.PGN_FRONT_IMU_FOUND:
                case PGNValues.PGN_FRONT_IMU_LOST:
                case PGNValues.PGN_FRONT_APRON_IMU_FOUND:
                case PGNValues.PGN_FRONT_APRON_IMU_LOST:
                case PGNValues.PGN_FRONT_BUCKET_IMU_FOUND:
                case PGNValues.PGN_FRONT_BUCKET_IMU_LOST:
                case PGNValues.PGN_REAR_IMU_FOUND:
                case PGNValues.PGN_REAR_IMU_LOST:
                case PGNValues.PGN_REAR_BUCKET_IMU_FOUND:
                case PGNValues.PGN_REAR_BUCKET_IMU_LOST:
                case PGNValues.PGN_FRONT_HEIGHT_FOUND:
                case PGNValues.PGN_FRONT_HEIGHT_LOST:
                case PGNValues.PGN_REAR_HEIGHT_FOUND:
                case PGNValues.PGN_REAR_HEIGHT_LOST:
                case PGNValues.PGN_FRONT_DUMPING:
                case PGNValues.PGN_REAR_DUMPING:
                case PGNValues.PGN_FRONT_CUTTING_REQUEST:
                case PGNValues.PGN_REAR_CUTTING_REQUEST:
                case PGNValues.PGN_FRONT_BLADE_JOG_UP:
                case PGNValues.PGN_FRONT_BLADE_JOG_DOWN:
                case PGNValues.PGN_REAR_BLADE_JOG_UP:
                case PGNValues.PGN_REAR_BLADE_JOG_DOWN:
                case PGNValues.PGN_YOU_ARE_SECONDARY:
                    urgent = true;
                    break;

                case PGNValues.PGN_FRONT_BLADE_OFFSET_SLAVE:
                    PendingFrontSlaveOffset = (Int16)stat.GetUInt32();
                    FrontSlaveOffsetDirty = true;
                    break;

                case PGNValues.PGN_REAR_BLADE_OFFSET_SLAVE:
                    PendingRearSlaveOffset = (Int16)stat.GetUInt32();
                    RearSlaveOffsetDirty = true;
                    break;

                case PGNValues.PGN_FRONT_BLADE_HEIGHT:
                    PendingFrontBladeHeight = stat.GetUInt32();
                    FrontBladeHeightDirty = true;
                    break;

                case PGNValues.PGN_REAR_BLADE_HEIGHT:
                    PendingRearBladeHeight = stat.GetUInt32();
                    RearBladeHeightDirty = true;
                    break;

                case PGNValues.PGN_FRONT_APRON_ANGLE:
                    PendingFrontApronAngle = (Int32)stat.GetUInt32() / 100.0;
                    FrontApronAngleDirty = true;
                    break;

                case PGNValues.PGN_FRONT_BUCKET_ANGLE:
                    PendingFrontBucketAngle = (Int32)stat.GetUInt32() / 100.0;
                    FrontBucketAngleDirty = true;
                    break;

                case PGNValues.PGN_REAR_BUCKET_ANGLE:
                    PendingRearBucketAngle = (Int32)stat.GetUInt32() / 100.0;
                    RearBucketAngleDirty = true;
                    break;

                case PGNValues.PGN_TRACTOR_IMU:
                    TractorIMU.Pitch = ((Int32)stat.GetUInt32(0)) / 100.0;
                    TractorIMU.Roll = ((Int32)stat.GetUInt32(4)) / 100.0;
                    TractorIMU.Heading = ((Int32)stat.GetUInt32(8)) / 100.0;
                    TractorIMU.YawRate = ((Int32)stat.GetUInt32(12)) / 100.0;
                    TractorIMU.CalibrationStatus = (IMUValue.Calibration)stat.GetByte(16);
                    TractorImuDirty = true;
                    break;

                case PGNValues.PGN_FRONT_IMU:
                    FrontScraperIMU.Pitch = ((Int32)stat.GetUInt32(0)) / 100.0;
                    FrontScraperIMU.Roll = ((Int32)stat.GetUInt32(4)) / 100.0;
                    FrontScraperIMU.Heading = ((Int32)stat.GetUInt32(8)) / 100.0;
                    FrontScraperIMU.YawRate = ((Int32)stat.GetUInt32(12)) / 100.0;
                    FrontScraperIMU.CalibrationStatus = (IMUValue.Calibration)stat.GetByte(16);
                    FrontImuDirty = true;
                    break;

                case PGNValues.PGN_FRONT_APRON_IMU:
                    FrontScraperApronIMU.Pitch = ((Int32)stat.GetUInt32(0)) / 100.0;
                    FrontScraperApronIMU.Roll = ((Int32)stat.GetUInt32(4)) / 100.0;
                    FrontScraperApronIMU.Heading = ((Int32)stat.GetUInt32(8)) / 100.0;
                    FrontScraperApronIMU.YawRate = ((Int32)stat.GetUInt32(12)) / 100.0;
                    FrontScraperApronIMU.CalibrationStatus = (IMUValue.Calibration)stat.GetByte(16);
                    FrontApronImuDirty = true;
                    break;

                case PGNValues.PGN_FRONT_BUCKET_IMU:
                    FrontScraperBucketIMU.Pitch = ((Int32)stat.GetUInt32(0)) / 100.0;
                    FrontScraperBucketIMU.Roll = ((Int32)stat.GetUInt32(4)) / 100.0;
                    FrontScraperBucketIMU.Heading = ((Int32)stat.GetUInt32(8)) / 100.0;
                    FrontScraperBucketIMU.YawRate = ((Int32)stat.GetUInt32(12)) / 100.0;
                    FrontScraperBucketIMU.CalibrationStatus = (IMUValue.Calibration)stat.GetByte(16);
                    FrontBucketImuDirty = true;
                    break;

                case PGNValues.PGN_REAR_IMU:
                    RearScraperIMU.Pitch = ((Int32)stat.GetUInt32(0)) / 100.0;
                    RearScraperIMU.Roll = ((Int32)stat.GetUInt32(4)) / 100.0;
                    RearScraperIMU.Heading = ((Int32)stat.GetUInt32(8)) / 100.0;
                    RearScraperIMU.YawRate = ((Int32)stat.GetUInt32(12)) / 100.0;
                    RearScraperIMU.CalibrationStatus = (IMUValue.Calibration)stat.GetByte(16);
                    RearImuDirty = true;
                    break;

                case PGNValues.PGN_REAR_BUCKET_IMU:
                    RearScraperBucketIMU.Pitch = ((Int32)stat.GetUInt32(0)) / 100.0;
                    RearScraperBucketIMU.Roll = ((Int32)stat.GetUInt32(4)) / 100.0;
                    RearScraperBucketIMU.Heading = ((Int32)stat.GetUInt32(8)) / 100.0;
                    RearScraperBucketIMU.YawRate = ((Int32)stat.GetUInt32(12)) / 100.0;
                    RearScraperBucketIMU.CalibrationStatus = (IMUValue.Calibration)stat.GetByte(16);
                    RearBucketImuDirty = true;
                    break;

                case PGNValues.PGN_FRONT_BLADE_DIRECTION:
                    PendingFrontBladeDirectionUp = stat.GetByte() == 1;
                    FrontBladeDirectionDirty = true;
                    break;

                case PGNValues.PGN_REAR_BLADE_DIRECTION:
                    PendingRearBladeDirectionUp = stat.GetByte() == 1;
                    RearBladeDirectionDirty = true;
                    break;

                case PGNValues.PGN_FRONT_BLADE_PWMVALUE:
                    PendingFrontBladePwm = stat.Data[0];
                    FrontBladePwmDirty = true;
                    break;

                case PGNValues.PGN_REAR_BLADE_PWMVALUE:
                    PendingRearBladePwm = stat.Data[0];
                    RearBladePwmDirty = true;
                    break;

                case PGNValues.PGN_TRACTOR_NMEA:
                    ProcessNMEA(
                        Encoding.ASCII.GetString(stat.Data),
                        ref TractorFix,
                        ref TractorVector,
                        null,
                        TractorIMU,
                        CurrentEquipmentSettings.TractorAntennaHeightMm,
                        CurrentEquipmentSettings.TractorAntennaLeftOffsetMm,
                        CurrentEquipmentSettings.TractorAntennaForwardOffsetMm);
                    TractorLocationDirty = true;
                    if (TractorFix != null) TractorQualityMonitor.Update(TractorFix);
                    break;

                case PGNValues.PGN_FRONT_NMEA:
                    ProcessNMEA(
                        Encoding.ASCII.GetString(stat.Data),
                        ref FrontFix,
                        ref FrontVector,
                        null,
                        FrontScraperIMU,
                        CurrentEquipmentSettings.FrontPan.AntennaHeightMm,
                        0,
                        0);
                    FrontLocationDirty = true;
                    if (FrontFix != null) FrontQualityMonitor.Update(FrontFix);
                    break;

                case PGNValues.PGN_REAR_NMEA:
                    ProcessNMEA(
                        Encoding.ASCII.GetString(stat.Data),
                        ref RearFix,
                        ref RearVector,
                        null,
                        RearScraperIMU,
                        CurrentEquipmentSettings.RearPan.AntennaHeightMm,
                        0,
                        0);
                    RearLocationDirty = true;
                    if (RearFix != null) RearQualityMonitor.Update(RearFix);
                    break;

                case PGNValues.PGN_ONBOARD_TRACTOR_IMU:
                    break;
            }
        }

        private void ProcessUrgentPacket(PGNPacket stat)
        {
            switch (stat.PGN)
            {
                case PGNValues.PGN_ESTOP:
                    OnEmergencyStop?.Invoke();
                    break;

                case PGNValues.PGN_CLEAR_ESTOP:
                    OnEmergencyStopCleared?.Invoke();
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

                case PGNValues.PGN_FRONT_APRON_IMU_FOUND:
                    OnIMUFound?.Invoke(EquipType.FrontApron);
                    break;

                case PGNValues.PGN_FRONT_APRON_IMU_LOST:
                    OnIMULost?.Invoke(EquipType.FrontApron);
                    break;

                case PGNValues.PGN_FRONT_BUCKET_IMU_FOUND:
                    OnIMUFound?.Invoke(EquipType.FrontBucket);
                    break;

                case PGNValues.PGN_FRONT_BUCKET_IMU_LOST:
                    OnIMULost?.Invoke(EquipType.FrontBucket);
                    break;

                case PGNValues.PGN_REAR_IMU_FOUND:
                    OnIMUFound?.Invoke(EquipType.Rear);
                    break;

                case PGNValues.PGN_REAR_IMU_LOST:
                    OnIMULost?.Invoke(EquipType.Rear);
                    break;

                case PGNValues.PGN_REAR_BUCKET_IMU_FOUND:
                    OnIMUFound?.Invoke(EquipType.RearBucket);
                    break;

                case PGNValues.PGN_REAR_BUCKET_IMU_LOST:
                    OnIMULost?.Invoke(EquipType.RearBucket);
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

                case PGNValues.PGN_FRONT_DUMPING:
                    OnFrontDumpingChanged?.Invoke(stat.GetByte() == 1);
                    break;

                case PGNValues.PGN_REAR_DUMPING:
                    OnRearDumpingChanged?.Invoke(stat.GetByte() == 1);
                    break;

                case PGNValues.PGN_FRONT_CUTTING_REQUEST:
                    OnFrontBladeCuttingChanged?.Invoke(stat.GetByte() == 1);
                    break;

                case PGNValues.PGN_REAR_CUTTING_REQUEST:
                    OnRearBladeCuttingChanged?.Invoke(stat.GetByte() == 1);
                    break;

                case PGNValues.PGN_FRONT_BLADE_JOG_UP:
                    OnFrontBladeJogged?.Invoke(true);
                    break;

                case PGNValues.PGN_FRONT_BLADE_JOG_DOWN:
                    OnFrontBladeJogged?.Invoke(false);
                    break;

                case PGNValues.PGN_REAR_BLADE_JOG_UP:
                    OnRearBladeJogged?.Invoke(true);
                    break;

                case PGNValues.PGN_REAR_BLADE_JOG_DOWN:
                    OnRearBladeJogged?.Invoke(false);
                    break;

                case PGNValues.PGN_YOU_ARE_SECONDARY:
                    OnEnableSecondaryTabletMode?.Invoke();
                    break;
            }
        }

        private void DispatchCoalescedUpdates()
        {
            if (PendingControllerFound)
            {
                PendingControllerFound = false;
                _IsControllerFound = true;
                OnControllerFound?.Invoke();
            }

            if (FrontSlaveOffsetDirty)
            {
                FrontSlaveOffsetDirty = false;
                OnFrontSlaveOffsetChanged?.Invoke(PendingFrontSlaveOffset);
            }

            if (RearSlaveOffsetDirty)
            {
                RearSlaveOffsetDirty = false;
                OnRearSlaveOffsetChanged?.Invoke(PendingRearSlaveOffset);
            }

            if (FrontBladeHeightDirty)
            {
                FrontBladeHeightDirty = false;
                OnFrontBladeHeightChanged?.Invoke(PendingFrontBladeHeight);
            }

            if (RearBladeHeightDirty)
            {
                RearBladeHeightDirty = false;
                OnRearBladeHeightChanged?.Invoke(PendingRearBladeHeight);
            }

            if (FrontApronAngleDirty)
            {
                FrontApronAngleDirty = false;
                OnFrontApronAngleChanged?.Invoke(PendingFrontApronAngle);
            }

            if (FrontBucketAngleDirty)
            {
                FrontBucketAngleDirty = false;
                OnFrontBucketAngleChanged?.Invoke(PendingFrontBucketAngle);
            }

            if (RearBucketAngleDirty)
            {
                RearBucketAngleDirty = false;
                OnRearBucketAngleChanged?.Invoke(PendingRearBucketAngle);
            }

            if (TractorImuDirty)
            {
                TractorImuDirty = false;
                OnTractorIMUChanged?.Invoke(TractorIMU);
            }

            if (FrontImuDirty)
            {
                FrontImuDirty = false;
                OnFrontIMUChanged?.Invoke(FrontScraperIMU);
            }

            if (FrontApronImuDirty)
            {
                FrontApronImuDirty = false;
                OnFrontApronIMUChanged?.Invoke(FrontScraperApronIMU);
            }

            if (FrontBucketImuDirty)
            {
                FrontBucketImuDirty = false;
                OnFrontBucketIMUChanged?.Invoke(FrontScraperBucketIMU);
            }

            if (RearImuDirty)
            {
                RearImuDirty = false;
                OnRearIMUChanged?.Invoke(RearScraperIMU);
            }

            if (RearBucketImuDirty)
            {
                RearBucketImuDirty = false;
                OnRearBucketIMUChanged?.Invoke(RearScraperBucketIMU);
            }

            if (FrontBladeDirectionDirty)
            {
                FrontBladeDirectionDirty = false;
                OnFrontBladeDirectionChanged?.Invoke(PendingFrontBladeDirectionUp);
            }

            if (RearBladeDirectionDirty)
            {
                RearBladeDirectionDirty = false;
                OnRearBladeDirectionChanged?.Invoke(PendingRearBladeDirectionUp);
            }

            if (FrontBladePwmDirty)
            {
                FrontBladePwmDirty = false;
                OnFrontBladePWMChanged?.Invoke(PendingFrontBladePwm);
            }

            if (RearBladePwmDirty)
            {
                RearBladePwmDirty = false;
                OnRearBladePWMChanged?.Invoke(PendingRearBladePwm);
            }

            if (TractorLocationDirty && TractorFix != null)
            {
                TractorLocationDirty = false;
                OnTractorLocationChanged?.Invoke(TractorFix);
            }

            if (FrontLocationDirty && FrontFix != null)
            {
                FrontLocationDirty = false;
                OnFrontLocationChanged?.Invoke(FrontFix);
            }

            if (RearLocationDirty && RearFix != null)
            {
                RearLocationDirty = false;
                OnRearLocationChanged?.Invoke(RearFix);
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
            if (Sentence.StartsWith("$GPGGA") || Sentence.StartsWith("$GNGGA"))
            {
                try
                {
                    Fix = GNSSFix.ParseNMEA(Sentence);
                    if (Vector != null)
                    {
                        Fix.Vector = Vector.Clone();
                    }

                    // sensor fusing is disabled - it takes place at the microcontroller level instead
                    //// fuse with IMU
                    //if ((TractorIMU.CalibrationStatus == IMUValue.Calibration.Good) || (TractorIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                    //{
                    //    Fix = Fusor.Fuse(Fix, IMU, AntennaHeightMm, AntennaLeftOffsetMm, AntennaForwardOffsetMm, MagneticDeclinationDegrees, MagneticDeclinationMinutes);
                    //}

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

                        // sensor fusing is disabled - it takes place at the microcontroller level instead
                        //// fuse with IMU
                        //if ((TractorIMU.CalibrationStatus == IMUValue.Calibration.Good) || (TractorIMU.CalibrationStatus == IMUValue.Calibration.Excellent))
                        //{
                        //    Fix = Fusor.Fuse(Fix, IMU, AntennaHeightMm, AntennaLeftOffsetMm, AntennaForwardOffsetMm, MagneticDeclinationDegrees, MagneticDeclinationMinutes);
                        //}

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
            byte State = (byte)Mode;

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_FRONT_STATE, new byte[] { State });
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
            byte State = (byte)Mode;

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_REAR_STATE, new byte[] { State });
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sets the front cut valve
        /// </summary>
        /// <param name="Value">CUTVALVE_MIN -> CUTVALVE_MAX with 200 = at target height</param>
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
        /// Sends the tractor antenna location
        /// </summary>
        /// <param name="HeightMm">Height of antenna in mm</param>
        /// <param name="LeftOffsetMm">Left/right offset of antenna in mm (left is positive)</param>
        /// <param name="ForwardOffsetMm">Forwards/rear offset of antenna in mm (forward is positive)</param>
        public void SetTractorAntennaLocation
            (
            uint HeightMm,
            int LeftOffsetMm,
            int ForwardOffsetMm
            )
        {
            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_TRACTOR_ANTENNA_HEIGHT, HeightMm);
            SendControllerCommand(TxCmd);
            TxCmd = new PGNPacket(PGNValues.PGN_TRACTOR_ANTENNA_LEFTOFF, (UInt32)LeftOffsetMm);
            SendControllerCommand(TxCmd);
            TxCmd = new PGNPacket(PGNValues.PGN_TRACTOR_ANTENNA_FORWARDOFF, (UInt32)ForwardOffsetMm);
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sends the magnetic declination
        /// </summary>
        /// <param name="Degrees">Declination degrees</param>
        /// <param name="Minutes">Declination minutes</param>
        public void SetMagneticDeclination
            (
            int Degrees,
            uint Minutes
            )
        {
            MagneticDeclinationDegrees = Degrees;
            MagneticDeclinationMinutes = Minutes;

            UInt32 Decl = (UInt32)((Degrees + (double)Minutes / 60.0) * 100);

            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_MAGNETIC_DECLINATION, Decl);
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sends the height of the front scraper antenna
        /// </summary>
        /// <param name="HeightMm">Height in mm</param>
        public void SetFrontAntennaHeight
            (
            uint HeightMm
            )
        {
            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_FRONT_ANTENNA_HEIGHT, HeightMm);
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sends the height of the rear scraper antenna
        /// </summary>
        /// <param name="HeightMm">Height in mm</param>
        public void SetRearAntennaHeight
            (
            uint HeightMm
            )
        {
            PGNPacket TxCmd = new PGNPacket(PGNValues.PGN_REAR_ANTENNA_HEIGHT, HeightMm);
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sets the rear cut valve
        /// </summary>
        /// <param name="Value">CUTVALVE_MIN -> CUTVALVE_MAX with 200 = at target height</param>
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

        /// <summary>
        /// Sets the orientation of an IMU
        /// </summary>
        /// <param name="IMU">IMU to set</param>
        /// <param name="Orientation">New orientation</param>
        public void SetIMUOrientation
            (
            IMUs IMU,
            IMUOrientations Orientation
            )
        {
            PGNValues PGN;

            switch (IMU)
            {
                default:
                case IMUs.Tractor:
                    PGN = PGNValues.PGN_TRACTOR_IMU_ORIENT;
                    break;

                case IMUs.Front:
                    PGN = PGNValues.PGN_FRONT_IMU_ORIENT;
                    break;

                case IMUs.Rear:
                    PGN = PGNValues.PGN_FRONT_IMU_ORIENT;
                    break;

                case IMUs.FrontApron:
                    PGN = PGNValues.PGN_FRONT_APRON_IMU_ORIENT;
                    break;

                case IMUs.FrontBucket:
                    PGN = PGNValues.PGN_FRONT_BUCKET_IMU_ORIENT;
                    break;

                case IMUs.RearBucket:
                    PGN = PGNValues.PGN_REAR_BUCKET_IMU_ORIENT;
                    break;
            }

            UInt32 Orient;

            switch (Orientation)
            {
                default:
                case IMUOrientations.HorizontalA:
                    Orient = 0;
                    break;

                case IMUOrientations.VerticalA:
                    Orient = 1;
                    break;
            }

            PGNPacket TxCmd = new PGNPacket(PGN, Orient);
            SendControllerCommand(TxCmd);
        }

        /// <summary>
        /// Sets an IMU as level
        /// </summary>
        /// <param name="IMU">IMU to set as level</param>
        public void SetIMULevel
            (
            IMUs IMU
            )
        {
            PGNValues PGN;

            switch (IMU)
            {
                default:
                case IMUs.Tractor:
                    PGN = PGNValues.PGN_TRACTOR_IMU_LEVEL;
                    break;

                case IMUs.Front:
                    PGN = PGNValues.PGN_FRONT_IMU_LEVEL;
                    break;

                case IMUs.Rear:
                    PGN = PGNValues.PGN_FRONT_IMU_LEVEL;
                    break;

                case IMUs.FrontApron:
                    PGN = PGNValues.PGN_FRONT_APRON_IMU_LEVEL;
                    break;

                case IMUs.FrontBucket:
                    PGN = PGNValues.PGN_FRONT_BUCKET_IMU_LEVEL;
                    break;

                case IMUs.RearBucket:
                    PGN = PGNValues.PGN_REAR_BUCKET_IMU_LEVEL;
                    break;
            }

            PGNPacket TxCmd = new PGNPacket(PGN);
            SendControllerCommand(TxCmd);
        }

        private void SendControllerCommand
            (
            PGNPacket Cmd
            )
        {
            if (ControllerChannel != null)
            {
                lock (this)
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
