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

        /// <summary>
        /// Called periodically to transmit pings telling AgGrade we are alive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PingTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            AgGradeStatus Ping = new AgGradeStatus();
            Ping.PGN = PGNValues.PGN_PING;
            Ping.Value = 0;
            uDPServer.Send(Ping);
        }

        private void UDPServer_OnCommandReceived
            (
            AgGradeCommand Command
            )
        {
        }

        private async void EStopBtn_Click(object sender, EventArgs e)
        {
            AgGradeStatus Status = new AgGradeStatus();
            Status.PGN = PGNValues.PGN_ESTOP;
            Status.Value = 0;

            await uDPServer.Send(Status);
        }

        private async void ClearEStopBtn_Click(object sender, EventArgs e)
        {
            AgGradeStatus Status = new AgGradeStatus();
            Status.PGN = PGNValues.PGN_CLEAR_ESTOP;
            Status.Value = 0;

            await uDPServer.Send(Status);
        }
    }
}
