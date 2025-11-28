using System;
using System.Text;
using Timer = System.Timers.Timer;

namespace HardwareSim
{
    public partial class MainForm : Form
    {
        // time between transmit of pings in milliseconds
        private const int PING_PERIOD_MS = 1000;

        private const double DEFAULT_LATITUDE = 36.448272;
        private const double DEFAULT_LONGITUDE = -90.724986;
        private const double DEFAULT_ALTITUDE = 0;

        private UDPServer uDPServer;
        private Timer PingTimer;
        private GNSS GNSSSim;

        public MainForm()
        {
            InitializeComponent();

            uDPServer = new UDPServer();
            uDPServer.OnCommandReceived += UDPServer_OnCommandReceived;
            uDPServer.StartListener();

            PingTimer = new Timer();
            PingTimer.Interval = PING_PERIOD_MS;
            PingTimer.Elapsed += PingTimer_Elapsed;
            PingTimer.Start();

            GNSSSim = new GNSS();
            GNSSSim.OnNewTractorFix += GNSSSim_OnNewTractorFix;

            LatitudeInput.Text = DEFAULT_LATITUDE.ToString();
            LongitudeInput.Text = DEFAULT_LONGITUDE.ToString();
            GNSSSim.SetTractorLocation(DEFAULT_LATITUDE, DEFAULT_LONGITUDE, DEFAULT_ALTITUDE, RTKQuality.RTKFloat);
            GNSSSim.SetTractorVector(10, 3);

            GNSSSim.Start();
        }

        private void GNSSSim_OnNewTractorFix(string NMEAString)
        {
            byte[] Data = Encoding.ASCII.GetBytes(NMEAString);
            SendStatus(new PGNPacket(PGNValues.PGN_TRACTOR_NMEA, Data));
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
        }

        private void UDPServer_OnCommandReceived
            (
            PGNPacket Command
            )
        {
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

            GNSSSim.SetTractorLocation(Lat, Lon, DEFAULT_ALTITUDE, RTKQuality.RTKFix);
        }
    }
}
