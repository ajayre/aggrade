using Microsoft.VisualBasic.Logging;
using System;
using System.Text;
using Timer = System.Timers.Timer;

namespace HardwareSim
{
    public partial class MainForm : Form
    {
        // time between transmit of pings in milliseconds
        private const int PING_PERIOD_MS = 1000;

        private const double DEFAULT_LATITUDE = 36.446847109944279;
        private const double DEFAULT_LONGITUDE = -90.72286177445794;
        private const double DEFAULT_ALTITUDE = 0;

        private UDPServer uDPServer;
        private Timer PingTimer;
        private GNSS GNSSSim;
        private bool FrontBladeAuto = false;
        private bool RearBladeAuto = false;
        private bool FrontDumping = false;
        private bool RearDumping = false;
        private DateTime? LastRxPingTime = null;

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
                    // we send back the new blade height
                    SendStatus(new PGNPacket(PGNValues.PGN_FRONT_BLADE_HEIGHT, Command.GetUInt32()));
                    break;

                case PGNValues.PGN_REAR_CUT_VALVE:
                    // we send back the new blade height
                    SendStatus(new PGNPacket(PGNValues.PGN_REAR_BLADE_HEIGHT, Command.GetUInt32()));
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
            GNSSSim.IncreaseSpeed();
        }

        private void ReverseBtn_Click(object sender, EventArgs e)
        {
            GNSSSim.DecreaseSpeed();
        }

        private void SteerLeftBtn_Click(object sender, EventArgs e)
        {
            GNSSSim.TurnLeft();
        }

        private void SteerRightBtn_Click(object sender, EventArgs e)
        {
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
    }
}
