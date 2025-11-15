using AgGrade.Controls;

namespace AgGrade
{
    public partial class MainForm : Form
    {
        public MainForm
            (
            string[] args
            )
        {
            bool FullScreen = false;

            InitializeComponent();

            if (args.Length > 0)
            {
                if (args[0].Trim().ToUpper() == @"/FULLSCREEN") FullScreen = true;
            }

            if (FullScreen)
            {
                // Set the form to not have a border, allowing it to cover the entire screen
                this.FormBorderStyle = FormBorderStyle.None;
                // Specify that the form's position and size will be set manually
                this.StartPosition = FormStartPosition.Manual;
                // Set the form's location to the top-left corner of the primary screen
                this.Location = Screen.PrimaryScreen!.Bounds.Location;
                // Set the form's size to the full resolution of the primary screen
                this.Size = Screen.PrimaryScreen.Bounds.Size;
            }
        }

        /// <summary>
        /// Called when user taps on the edit equipment button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditEquipmentBtn_Click(object sender, EventArgs e)
        {
            ShowEditEquipment();
        }

        /// <summary>
        /// Shows the UI for editing the equipment
        /// </summary>
        private void ShowEditEquipment
            (
            )
        {
            EquipmentEditor equipmentEditor = new EquipmentEditor();
            equipmentEditor.Parent = ContentPanel;
            equipmentEditor.Dock = DockStyle.Fill;
            equipmentEditor.Show();
        }
    }
}
