using System;
using Timer = System.Timers.Timer;

namespace HardwareSim
{
    public partial class MainForm : Form
    {
        // time between transmit of pings in milliseconds
        private const int PING_PERIOD_MS = 1000;

        private UDPServer uDPServer;
        private Timer PingTimer;

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
        }

        private async void SendStatus
            (
            PGNValues PGN,
            UInt32 Value
            )
        {
            AgGradeStatus Status = new AgGradeStatus();
            Status.PGN = PGN;
            Status.Value = Value;

            await uDPServer.Send(Status);
        }

        /// <summary>
        /// Called periodically to transmit pings telling AgGrade we are alive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            SendStatus(PGNValues.PGN_PING, 0);
        }

        private void UDPServer_OnCommandReceived
            (
            AgGradeCommand Command
            )
        {
        }

        private void EStopBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_ESTOP, 0);
        }

        private void ClearEStopBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_CLEAR_ESTOP, 0);
        }

        private void TractorIMUFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_TRACTOR_IMU_FOUND, 0);
        }

        private void TractorIMULostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_TRACTOR_IMU_LOST, 0);
        }

        private void FrontIMUFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_FRONT_IMU_FOUND, 0);
        }

        private void FrontIMULostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_FRONT_IMU_LOST, 0);
        }

        private void RearIMUFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_REAR_IMU_FOUND, 0);
        }

        private void RearIMULostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_REAR_IMU_LOST, 0);
        }

        private void FrontHeightFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_FRONT_HEIGHT_FOUND, 0);
        }

        private void FrontHeightLostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_FRONT_HEIGHT_LOST, 0);
        }

        private void RearHeightFoundBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_REAR_HEIGHT_FOUND, 0);
        }

        private void RearHeightLostBtn_Click(object sender, EventArgs e)
        {
            SendStatus(PGNValues.PGN_REAR_HEIGHT_LOST, 0);
        }
    }
}
