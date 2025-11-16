using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class EquipmentEditor : UserControl
    {
        public EquipmentEditor()
        {
            InitializeComponent();

            ShowVehicleSettings();
        }

        /// <summary>
        /// Called when section button is clicked
        /// Shows the settings for that section
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SectionBtn_Click(object sender, EventArgs e)
        {
            if (sender == VehicleBtn)
            {
                ShowVehicleSettings();
            }
            else if (sender == FrontPanBtn)
            {
                ShowFrontPanSettings();
            }
            else
            {
                ShowRearPanSettings();
            }
        }

        private void ShowVehicleSettings
            (
            )
        {
            SelectButton(VehicleBtn);
            DeselectButton(FrontPanBtn);
            DeselectButton(RearPanBtn);

            ContentPanel.Controls.Clear();
            VehicleSettingsEditor Editor = new VehicleSettingsEditor();
            Editor.Parent = ContentPanel;
            Editor.Dock = DockStyle.Fill;
            Editor.Show();
        }

        private void ShowFrontPanSettings
            (
            )
        {
            DeselectButton(VehicleBtn);
            SelectButton(FrontPanBtn);
            DeselectButton(RearPanBtn);

            ContentPanel.Controls.Clear();
        }

        private void ShowRearPanSettings
            (
            )
        {
            DeselectButton(VehicleBtn);
            DeselectButton(FrontPanBtn);
            SelectButton(RearPanBtn);

            ContentPanel.Controls.Clear();
        }

        /// <summary>
        /// Changes the display of a button to show it as selected
        /// </summary>
        /// <param name="Btn">Button to change</param>
        private void SelectButton
            (
            Button Btn
            )
        {
            Btn.ForeColor = Color.Black;
            Btn.BackColor = Color.DarkOrange;
        }

        /// <summary>
        /// Changes the display of a button to show it as deselected
        /// </summary>
        /// <param name="Btn">Button to change</param>
        private void DeselectButton
            (
            Button Btn
            )
        {
            Btn.BackColor = Color.Green;
            Btn.ForeColor = SystemColors.Control;
        }
    }
}
