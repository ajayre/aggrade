using Microsoft.VisualBasic.Logging;
using System;
using System.Text;
using Timer = System.Timers.Timer;

namespace HardwareSim
{
    public partial class MainForm : Form
    {
        private enum JogDirections
        {
            Up,
            Down
        }

        // time between transmit of pings in milliseconds
        private const int PING_PERIOD_MS = 1000;

        public const int BLADE_HEIGHT_GROUND_LEVEL = 200;

        // time between transmits of jog messages in milliseconds
        private const int INITIAL_JOG_PERIOD_MS = 250;
        private const int RECURRING_JOG_PERIOD_MS = 100;

        private const double DEFAULT_LATITUDE = 36.446847109944279;
        private const double DEFAULT_LONGITUDE = -90.72286177445794;
        private const double DEFAULT_ALTITUDE = 0;
        private const double AUTO_DRIVE_MIN_SPEED_MPH = 1.0;
        private const double AUTO_DRIVE_MAX_SPEED_MPH = 15.0;
        private const double AUTO_DRIVE_ACCEL_MPH_PER_SEC = 1.75;
        private const double AUTO_DRIVE_TURN_DEG_PER_SEC = 12.0;
        private const int AUTO_DRIVE_PERIOD_MS = 100;
        private const double AUTO_DRIVE_AREA_ACRES = 40.0;
        private const double AUTO_DRIVE_EDGE_BUFFER_M = 15.0;
        private const double AUTO_DRIVE_EDGE_RECOVERY_M = 35.0;

        private UDPServer uDPServer;
        private Timer PingTimer;
        private GNSS GNSSSim;
        private bool FrontBladeAuto = false;
        private bool RearBladeAuto = false;
        private bool FrontDumping = false;
        private bool RearDumping = false;
        private DateTime? LastRxPingTime = null;
        private UInt32 FrontBladeHeight = BLADE_HEIGHT_GROUND_LEVEL;
        private UInt32 RearBladeHeight = BLADE_HEIGHT_GROUND_LEVEL;
        private Timer FrontJogTimer;
        private Timer RearJogTimer;
        private JogDirections FrontJogDirection;
        private JogDirections RearJogDirection;
        private readonly Random Randomizer = new Random();
        private System.Windows.Forms.Timer AutoDriveTimer;
        private bool AutoDriveEnabled = false;
        private DateTime AutoDriveNextPlanTime = DateTime.MinValue;
        private double AutoDriveTargetSpeedMPH = AUTO_DRIVE_MIN_SPEED_MPH;
        private double AutoDriveTargetHeadingDeg = 0;
        private double AutoDriveCenterLatitude;
        private double AutoDriveCenterLongitude;
        private readonly double AutoDriveSquareHalfSideM = Math.Sqrt(AUTO_DRIVE_AREA_ACRES * 4046.8564224) / 2.0;

        public MainForm()
        {
            InitializeComponent();

            GNSSSim = new GNSS();
            GNSSSim.OnNewTractorFix += GNSSSim_OnNewTractorFix;
            GNSSSim.OnNewFrontFix += GNSSSim_OnNewFrontFix;
            GNSSSim.OnNewRearFix += GNSSSim_OnNewRearFix;
            GNSSSim.OnNewTractorIMU += GNSSSim_OnNewTractorIMU;
            GNSSSim.OnNewFrontIMU += GNSSSim_OnNewFrontIMU;
            GNSSSim.OnNewRearIMU += GNSSSim_OnNewRearIMU;

            uDPServer = new UDPServer();
            uDPServer.OnCommandReceived += UDPServer_OnCommandReceived;
            uDPServer.OnAgGradeClosed += UDPServer_OnAgGradeClosed;
            uDPServer.StartListener();

            PingTimer = new Timer();
            PingTimer.Interval = PING_PERIOD_MS;
            PingTimer.Elapsed += PingTimer_Elapsed;
            PingTimer.Start();

            FrontJogTimer = new Timer();
            FrontJogTimer.Interval = INITIAL_JOG_PERIOD_MS;
            FrontJogTimer.Elapsed += FrontJogTimer_Elapsed;

            RearJogTimer = new Timer();
            RearJogTimer.Interval = INITIAL_JOG_PERIOD_MS;
            RearJogTimer.Elapsed += RearJogTimer_Elapsed;

            AutoDriveTimer = new System.Windows.Forms.Timer();
            AutoDriveTimer.Interval = AUTO_DRIVE_PERIOD_MS;
            AutoDriveTimer.Tick += AutoDriveTimer_Tick;

            LatitudeInput.Text = DEFAULT_LATITUDE.ToString();
            LongitudeInput.Text = DEFAULT_LONGITUDE.ToString();
        }

        /// <summary>
        /// AgGrade has closed, start listening for new connection
        /// </summary>
        private void UDPServer_OnAgGradeClosed()
        {
            uDPServer.StartListener();
        }

        private void GNSSSim_OnNewRearIMU(IMUValue Value)
        {
            byte[] Data = new byte[PGNPacket.MAX_LEN];
            PGNPacket Packet = new PGNPacket();
            Packet.PGN = PGNValues.PGN_FRONT_IMU;
            Packet.SetUInt32(0, (UInt32)(Value.Pitch * 100));
            Packet.SetUInt32(4, (UInt32)(Value.Roll * 100));
            Packet.SetUInt32(8, (UInt32)(Value.Heading * 100));
            Packet.SetUInt32(12, (UInt32)(Value.YawRate * 100));
            Packet.Data[16] = (byte)Value.CalibrationStatus;
            SendStatus(Packet);
        }

        private void GNSSSim_OnNewFrontIMU(IMUValue Value)
        {
            byte[] Data = new byte[PGNPacket.MAX_LEN];
            PGNPacket Packet = new PGNPacket();
            Packet.PGN = PGNValues.PGN_REAR_IMU;
            Packet.SetUInt32(0, (UInt32)(Value.Pitch * 100));
            Packet.SetUInt32(4, (UInt32)(Value.Roll * 100));
            Packet.SetUInt32(8, (UInt32)(Value.Heading * 100));
            Packet.SetUInt32(12, (UInt32)(Value.YawRate * 100));
            Packet.Data[16] = (byte)Value.CalibrationStatus;
            SendStatus(Packet);
        }

        private void GNSSSim_OnNewTractorIMU(IMUValue Value)
        {
            byte[] Data = new byte[PGNPacket.MAX_LEN];
            PGNPacket Packet = new PGNPacket();
            Packet.PGN = PGNValues.PGN_TRACTOR_IMU;
            Packet.SetUInt32(0, (UInt32)(Value.Pitch * 100));
            Packet.SetUInt32(4, (UInt32)(Value.Roll * 100));
            Packet.SetUInt32(8, (UInt32)(Value.Heading * 100));
            Packet.SetUInt32(12, (UInt32)(Value.YawRate * 100));
            Packet.Data[16] = (byte)Value.CalibrationStatus;
            SendStatus(Packet);
        }

        private void GNSSSim_OnNewTractorFix(string NMEAString)
        {
            byte[] Data = Encoding.ASCII.GetBytes(NMEAString);
            SendStatus(new PGNPacket(PGNValues.PGN_TRACTOR_NMEA, Data));
        }

        private void GNSSSim_OnNewRearFix(string NMEAString)
        {
            byte[] Data = Encoding.ASCII.GetBytes(NMEAString);
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_NMEA, Data));
        }

        private void GNSSSim_OnNewFrontFix(string NMEAString)
        {
            byte[] Data = Encoding.ASCII.GetBytes(NMEAString);
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_NMEA, Data));
        }

        private async void SendStatus
            (
            PGNPacket Packet
            )
        {
            await uDPServer.Send(Packet);
        }

        /// <summary>
        /// Called periodically to transmit pings telling AgGrade we are alive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_PING));

            // AgGrade has dissappeared
            if (LastRxPingTime.HasValue && (DateTime.Now > LastRxPingTime.Value.AddMilliseconds(5000)))
            {
                LastRxPingTime = null;
            }
        }

        private void UDPServer_OnCommandReceived
            (
            PGNPacket Command
            )
        {
            switch (Command.PGN)
            {
                case PGNValues.PGN_PING:
                    LastRxPingTime = DateTime.Now;
                    break;

                case PGNValues.PGN_AGGRADE_STARTED:
                    double Lat = DEFAULT_LATITUDE;
                    double Lon = DEFAULT_LONGITUDE;
                    GNSSSim.SetTractorLocation(Lat, Lon, DEFAULT_ALTITUDE, RTKQuality.RTKFix, true);

                    SendStatus(new PGNPacket(PGNValues.PGN_TRACTOR_IMU_FOUND));
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_IMU_FOUND));
                    SendStatus(new PGNPacket(PGNValues.PGN_REAR_IMU_FOUND));
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_HEIGHT_FOUND));
                    SendStatus(new PGNPacket(PGNValues.PGN_REAR_HEIGHT_FOUND));

                    /*SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_HEIGHT, 5));
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_OFFSET_SLAVE, 6));
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_PWMVALUE, 127));
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_DIRECTION, 1));
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_AUTO, 1));*/

                    GNSSSim.Start();
                    break;

                case PGNValues.PGN_FRONT_CUT_VALVE:
                    FrontBladeHeight = Command.GetUInt32();
                    // we send back the new blade height
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_HEIGHT, FrontBladeHeight));
                    break;

                case PGNValues.PGN_REAR_CUT_VALVE:
                    RearBladeHeight = Command.GetUInt32();
                    // we send back the new blade height
                    SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_HEIGHT, RearBladeHeight));
                    break;

                case PGNValues.PGN_FRONT_BLADE_HEIGHT:
                    // send back the current blade height
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_HEIGHT, FrontBladeHeight));
                    break;

                case PGNValues.PGN_REAR_BLADE_HEIGHT:
                    // send back the current blade height
                    SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_HEIGHT, RearBladeHeight));
                    break;

                case PGNValues.PGN_FRONT_ZERO_BLADE_HEIGHT:
                    FrontBladeHeight = BLADE_HEIGHT_GROUND_LEVEL;
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_HEIGHT, FrontBladeHeight));
                    break;

                case PGNValues.PGN_REAR_ZERO_BLADE_HEIGHT:
                    RearBladeHeight = BLADE_HEIGHT_GROUND_LEVEL;
                    SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_HEIGHT, RearBladeHeight));
                    break;
            }
        }

        private void EStopBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_ESTOP));
        }

        private void ClearEStopBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_CLEAR_ESTOP));
        }

        private void TractorIMUFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_TRACTOR_IMU_FOUND));
        }

        private void TractorIMULostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_TRACTOR_IMU_LOST));
        }

        private void FrontIMUFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_IMU_FOUND));
        }

        private void FrontIMULostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_IMU_LOST));
        }

        private void RearIMUFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_IMU_FOUND));
        }

        private void RearIMULostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_IMU_LOST));
        }

        private void FrontHeightFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_HEIGHT_FOUND));
        }

        private void FrontHeightLostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_HEIGHT_LOST));
        }

        private void RearHeightFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_HEIGHT_FOUND));
        }

        private void RearHeightLostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_HEIGHT_LOST));
        }

        private void SetLocationBtn_Click(object sender, EventArgs e)
        {
            double Lat = double.Parse(LatitudeInput.Text);
            double Lon = double.Parse(LongitudeInput.Text);

            GNSSSim.SetTractorLocation(Lat, Lon, DEFAULT_ALTITUDE, RTKQuality.RTKFix, true);
        }

        private void ForwardsBtn_Click(object sender, EventArgs e)
        {
            StopAutoDrive();
            GNSSSim.IncreaseSpeed();
        }

        private void ReverseBtn_Click(object sender, EventArgs e)
        {
            StopAutoDrive();
            GNSSSim.DecreaseSpeed();
        }

        private void SteerLeftBtn_Click(object sender, EventArgs e)
        {
            StopAutoDrive();
            GNSSSim.TurnLeft();
        }

        private void SteerRightBtn_Click(object sender, EventArgs e)
        {
            StopAutoDrive();
            GNSSSim.TurnRight();
        }

        private void FrontToggleCuttingBtn_Click(object sender, EventArgs e)
        {
            FrontBladeAuto = !FrontBladeAuto;

            byte[] Data = new byte[1];
            Data[0] = (byte)(FrontBladeAuto ? 1 : 0);
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_AUTO, Data));
        }

        private void RearToggleCuttingBtn_Click(object sender, EventArgs e)
        {
            RearBladeAuto = !RearBladeAuto;

            byte[] Data = new byte[1];
            Data[0] = (byte)(RearBladeAuto ? 1 : 0);
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_AUTO, Data));
        }

        private void FrontToggleDumpingBtn_Click(object sender, EventArgs e)
        {
            FrontDumping = !FrontDumping;

            byte[] Data = new byte[1];
            Data[0] = (byte)(FrontDumping ? 1 : 0);
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_DUMPING, Data));
        }

        private void RearToggleDumpingBtn_Click(object sender, EventArgs e)
        {
            RearDumping = !RearDumping;

            byte[] Data = new byte[1];
            Data[0] = (byte)(RearDumping ? 1 : 0);
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_DUMPING, Data));
        }

        private void FrontJoystickUpBtn_MouseDown(object sender, MouseEventArgs e)
        {
            FrontJogDirection = JogDirections.Up;
            FrontJogTimer.Interval = INITIAL_JOG_PERIOD_MS;
            FrontJogTimer.Start();
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_JOG_UP));
        }

        private void FrontJoystickUpBtn_MouseUp(object sender, MouseEventArgs e)
        {
            FrontJogTimer.Stop();
        }

        private void FrontJogTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (FrontJogDirection == JogDirections.Up)
            {
                SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_JOG_UP));
            }
            else
            {
                SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_JOG_DOWN));
            }

            FrontJogTimer.Interval = RECURRING_JOG_PERIOD_MS;
        }

        private void FrontJoystickDownBtn_MouseUp(object sender, MouseEventArgs e)
        {
            FrontJogTimer.Stop();
        }

        private void FrontJoystickDownBtn_MouseDown(object sender, MouseEventArgs e)
        {
            FrontJogDirection = JogDirections.Down;
            FrontJogTimer.Interval = INITIAL_JOG_PERIOD_MS;
            FrontJogTimer.Start();
            SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_JOG_DOWN));
        }

        private void RearJogTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (RearJogDirection == JogDirections.Up)
            {
                SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_JOG_UP));
            }
            else
            {
                SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_JOG_DOWN));
            }

            RearJogTimer.Interval = RECURRING_JOG_PERIOD_MS;
        }

        private void RearJoystickUpBtn_MouseUp(object sender, MouseEventArgs e)
        {
            RearJogTimer.Stop();
        }

        private void RearJoystickUpBtn_MouseDown(object sender, MouseEventArgs e)
        {
            RearJogDirection = JogDirections.Up;
            RearJogTimer.Interval = INITIAL_JOG_PERIOD_MS;
            RearJogTimer.Start();
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_JOG_UP));
        }

        private void RearJoystickDownBtn_MouseDown(object sender, MouseEventArgs e)
        {
            RearJogDirection = JogDirections.Down;
            RearJogTimer.Interval = INITIAL_JOG_PERIOD_MS;
            RearJogTimer.Start();
            SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_JOG_DOWN));
        }

        private void RearJoystickDownBtn_MouseUp(object sender, MouseEventArgs e)
        {
            RearJogTimer.Stop();
        }

        private void AutoDriveBtn_Click(object sender, EventArgs e)
        {
            if (AutoDriveEnabled)
            {
                StopAutoDrive();
                return;
            }

            AutoDriveEnabled = true;
            AutoDriveBtn.Text = "Stop Auto";
            AutoDriveCenterLatitude = GNSSSim.TractorGNSS.Latitude;
            AutoDriveCenterLongitude = GNSSSim.TractorGNSS.Longitude;
            AutoDriveTargetSpeedMPH = Clamp(Randomizer.NextDouble() * (AUTO_DRIVE_MAX_SPEED_MPH - AUTO_DRIVE_MIN_SPEED_MPH) + AUTO_DRIVE_MIN_SPEED_MPH, AUTO_DRIVE_MIN_SPEED_MPH, AUTO_DRIVE_MAX_SPEED_MPH);
            AutoDriveTargetHeadingDeg = NormalizeHeading(GNSSSim.TractorGNSS.TrueHeading);
            AutoDriveNextPlanTime = DateTime.MinValue;
            AutoDriveTimer.Start();
        }

        private void StopAutoDrive()
        {
            if (!AutoDriveEnabled)
            {
                return;
            }

            AutoDriveEnabled = false;
            AutoDriveTimer.Stop();
            AutoDriveBtn.Text = "Auto Drive";
        }

        private void AutoDriveTimer_Tick(object? sender, EventArgs e)
        {
            if (!AutoDriveEnabled)
            {
                return;
            }

            double dtSec = AUTO_DRIVE_PERIOD_MS / 1000.0;
            (double xM, double yM) = GetOffsetFromCenterMeters(GNSSSim.TractorGNSS.Latitude, GNSSSim.TractorGNSS.Longitude);
            double nearestEdgeDistanceM = AutoDriveSquareHalfSideM - Math.Max(Math.Abs(xM), Math.Abs(yM));

            bool needsInwardCorrection = nearestEdgeDistanceM <= AUTO_DRIVE_EDGE_RECOVERY_M;
            bool shouldReplan =
                (DateTime.UtcNow >= AutoDriveNextPlanTime) ||
                (Math.Abs(AutoDriveTargetSpeedMPH - GNSSSim.TractorGNSS.SpeedMPH) < 0.5 &&
                 Math.Abs(ShortestHeadingDeltaDeg(GNSSSim.TractorGNSS.TrueHeading, AutoDriveTargetHeadingDeg)) < 5.0) ||
                needsInwardCorrection;

            if (shouldReplan)
            {
                ChooseNextAutoDrivePlan(xM, yM, needsInwardCorrection);
            }

            double nextSpeed = MoveTowards(
                GNSSSim.TractorGNSS.SpeedMPH,
                AutoDriveTargetSpeedMPH,
                AUTO_DRIVE_ACCEL_MPH_PER_SEC * dtSec);
            SetAllVehicleSpeeds(nextSpeed);

            double nextHeading = MoveTowardsHeading(
                GNSSSim.TractorGNSS.TrueHeading,
                AutoDriveTargetHeadingDeg,
                AUTO_DRIVE_TURN_DEG_PER_SEC * dtSec);
            GNSSSim.TractorGNSS.TrueHeading = nextHeading;

            // Emergency correction if the current point is very close to the boundary.
            if (nearestEdgeDistanceM <= AUTO_DRIVE_EDGE_BUFFER_M)
            {
                AutoDriveTargetHeadingDeg = ComputeInwardHeading(xM, yM);
                AutoDriveTargetSpeedMPH = Math.Min(AutoDriveTargetSpeedMPH, 6.0);
            }
        }

        private void ChooseNextAutoDrivePlan(double xM, double yM, bool forceInwardHeading)
        {
            double targetSpeed = Clamp(
                AUTO_DRIVE_MIN_SPEED_MPH + Randomizer.NextDouble() * (AUTO_DRIVE_MAX_SPEED_MPH - AUTO_DRIVE_MIN_SPEED_MPH),
                AUTO_DRIVE_MIN_SPEED_MPH,
                AUTO_DRIVE_MAX_SPEED_MPH);

            double targetHeading;
            if (forceInwardHeading)
            {
                targetHeading = ComputeInwardHeading(xM, yM);
            }
            else
            {
                targetHeading = FindRandomHeadingInsideBoundary(xM, yM, targetSpeed);
            }

            AutoDriveTargetSpeedMPH = targetSpeed;
            AutoDriveTargetHeadingDeg = targetHeading;
            AutoDriveNextPlanTime = DateTime.UtcNow.AddSeconds(2 + Randomizer.Next(0, 4));
        }

        private double FindRandomHeadingInsideBoundary(double xM, double yM, double targetSpeedMph)
        {
            const int maxAttempts = 20;
            double horizonSec = 6.0 + Randomizer.NextDouble() * 8.0;
            double horizonDistanceM = targetSpeedMph * 0.44704 * horizonSec;
            double maxAxis = AutoDriveSquareHalfSideM - AUTO_DRIVE_EDGE_BUFFER_M;

            for (int i = 0; i < maxAttempts; i++)
            {
                double heading = Randomizer.NextDouble() * 360.0;
                double projectedXM = xM + Math.Sin(ToRadians(heading)) * horizonDistanceM;
                double projectedYM = yM + Math.Cos(ToRadians(heading)) * horizonDistanceM;

                if (Math.Abs(projectedXM) <= maxAxis && Math.Abs(projectedYM) <= maxAxis)
                {
                    return heading;
                }
            }

            return ComputeInwardHeading(xM, yM);
        }

        private double ComputeInwardHeading(double xM, double yM)
        {
            double headingToCenter = ToDegrees(Math.Atan2(-xM, -yM));
            double randomOffset = -25.0 + (Randomizer.NextDouble() * 50.0);
            return NormalizeHeading(headingToCenter + randomOffset);
        }

        private (double xM, double yM) GetOffsetFromCenterMeters(double latitude, double longitude)
        {
            double meanLatRad = ToRadians((AutoDriveCenterLatitude + latitude) / 2.0);
            double metersPerDegLat = 111132.0;
            double metersPerDegLon = 111320.0 * Math.Cos(meanLatRad);

            double yM = (latitude - AutoDriveCenterLatitude) * metersPerDegLat;
            double xM = (longitude - AutoDriveCenterLongitude) * metersPerDegLon;
            return (xM, yM);
        }

        private void SetAllVehicleSpeeds(double speedMph)
        {
            double clampedSpeed = Clamp(speedMph, AUTO_DRIVE_MIN_SPEED_MPH, AUTO_DRIVE_MAX_SPEED_MPH);
            GNSSSim.TractorGNSS.SpeedMPH = clampedSpeed;
            GNSSSim.FrontGNSS.SpeedMPH = clampedSpeed;
            GNSSSim.RearGNSS.SpeedMPH = clampedSpeed;
        }

        private static double MoveTowards(double current, double target, double maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta)
            {
                return target;
            }

            return current + Math.Sign(target - current) * maxDelta;
        }

        private static double MoveTowardsHeading(double currentDeg, double targetDeg, double maxDeltaDeg)
        {
            double delta = ShortestHeadingDeltaDeg(currentDeg, targetDeg);
            if (Math.Abs(delta) <= maxDeltaDeg)
            {
                return NormalizeHeading(targetDeg);
            }

            return NormalizeHeading(currentDeg + Math.Sign(delta) * maxDeltaDeg);
        }

        private static double ShortestHeadingDeltaDeg(double currentDeg, double targetDeg)
        {
            double delta = NormalizeHeading(targetDeg) - NormalizeHeading(currentDeg);
            if (delta > 180.0) delta -= 360.0;
            if (delta < -180.0) delta += 360.0;
            return delta;
        }

        private static double NormalizeHeading(double headingDeg)
        {
            double normalized = headingDeg % 360.0;
            if (normalized < 0) normalized += 360.0;
            return normalized;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static double ToDegrees(double radians)
        {
            return radians * 180.0 / Math.PI;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
