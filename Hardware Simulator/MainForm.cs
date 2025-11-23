
namespace HardwareSim
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            UDPServer uDPServer = new UDPServer();
            uDPServer.StartListener();
        }
    }
}
