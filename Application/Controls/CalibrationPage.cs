using AgGrade.Controller;
using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class CalibrationPage : UserControl
    {
        private Panel? OptionsTable = null;
        private Wizard? Wizard = null;

        public EquipmentSettings CurrentEquipmentSettings;
        public EquipmentStatus CurrentEquipmentStatus;
        public Field? CurrentField;
        public OGController Controller;

        public Color FrontPanColor = Color.Black;
        public Color RearPanColor = Color.Black;

        public event Action OnEnableBladeLimits = null;
        public event Action OnDisableBladeLimits = null;

        public CalibrationPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Hides the list of calibration options
        /// </summary>
        private void HideOptions
            (
            )
        {
            if (OptionsTable != null)
            {
                if (Content.Controls.Contains(OptionsTable)) Content.Controls.Remove(OptionsTable);
                OptionsTable = null;
            }
        }

        /// <summary>
        /// Shows the list of calibration options
        /// </summary>
        private void ShowOptions
            (
            )
        {
            OptionsTable = new Panel();
            OptionsTable.Width = Content.Width - 44;
            OptionsTable.Height = Content.Height - 44;
            OptionsTable.Left = 22;
            OptionsTable.Top = 22;
            OptionsTable.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OptionsTable.Visible = true;
            OptionsTable.Parent = Content;
            Content.Controls.Add(OptionsTable);

            OptionsTable.Controls.Clear();

            // Add in reverse order so first sorted item appears at top (DockStyle.Top stacks first-added at bottom)
            bool odd = false;

            AddButton("Calibrate Tractor Antenna Location", Properties.Resources.tractor_48px, odd, CalibrateTractorAntenna);
            odd = !odd;
            if (CurrentEquipmentSettings.RearPan.Equipped)
            {
                AddButton("Calibrate Rear Blade Height", Properties.Resources.blade_48px, odd, CalibrateRearBladeHeight);
                odd = !odd;
            }
            if (CurrentEquipmentSettings.FrontPan.Equipped)
            {
                AddButton("Calibrate Front Blade Height", Properties.Resources.blade_48px, odd, CalibrateFrontBladeHeight);
                odd = !odd;
            }
            if ((CurrentField != null) && (CurrentField.Benchmarks.Count > 0))
            {
                AddButton("Calibrate Field Location", Properties.Resources.field_48px, odd, CalibrateFieldLocation);
                odd = !odd;
            }
        }

        /// <summary>
        /// Hides the wizard
        /// </summary>
        private void HideWizard
            (
            )
        {
            if (Wizard != null)
            {
                if (Content.Controls.Contains(Wizard)) { Content.Controls.Remove(Wizard); }
                Wizard.ExitWizard -= Wizard_ExitWizard;
                Wizard = null;
            }
        }

        /// <summary>
        /// Shows the wizard
        /// </summary>
        private void ShowWizard
            (
            )
        {
            Wizard = new Wizard();
            Wizard.Parent = Content;
            Wizard.Dock = DockStyle.Fill;
            Wizard.CurrentEquipmentStatus = CurrentEquipmentStatus;
            Wizard.CurrentEquipmentSettings = CurrentEquipmentSettings;
            Wizard.Controller = Controller;
            Wizard.ExitWizard += Wizard_ExitWizard;
            Content.Controls.Add(Wizard);
        }

        /// <summary>
        /// Called when the wizard requests to exit
        /// </summary>
        /// <param name="obj"></param>
        private void Wizard_ExitWizard(object obj)
        {
            HideWizard();
            ShowOptions();

            OnEnableBladeLimits?.Invoke();
        }

        /// <summary>
        /// Called when user taps on the button to calibrate the field location
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateFieldLocation
            (
            object Sender
            )
        {
            OnEnableBladeLimits?.Invoke();
        }

        /// <summary>
        /// Called when user taps on the button to calibrate the tractor antenna
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateTractorAntenna
            (
            object Sender
            )
        {
            HideOptions();
            ShowWizard();
            Wizard!.Name = "Tractor Antenna Location";
            Wizard!.Content = new CalibrateTractorAntennaWizard();
            OnEnableBladeLimits?.Invoke();
        }

        /// <summary>
        /// Called when the user taps on the button to calibrate the front blade height
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateFrontBladeHeight
            (
            object Sender
            )
        {
            HideOptions();
            ShowWizard();
            Wizard!.Name = "Front Blade Height";
            Wizard!.Content = new CalibrateBladeHeightWizard(CalibrateBladeHeightWizard.Blades.Front, FrontPanColor);
            OnDisableBladeLimits?.Invoke();
        }

        /// <summary>
        /// Called when the user taps on the button to calibrate the rear blade height
        /// </summary>
        /// <param name="Sender"></param>
        private void CalibrateRearBladeHeight
            (
            object Sender
            )
        {
            HideOptions();
            ShowWizard();
            Wizard!.Name = "Rear Blade Height";
            Wizard!.Content = new CalibrateBladeHeightWizard(CalibrateBladeHeightWizard.Blades.Rear, RearPanColor);
            OnDisableBladeLimits?.Invoke();
        }

        /// <summary>
        /// Adds a new button
        /// </summary>
        /// <param name="Text">Text to display on button</param>
        /// <param name="Icon">Icon to show</param>
        /// <param name="Odd">true to use secondary color</param>
        /// <param name="OnClicked">Handler when button is clicked/tapped</param>
        private void AddButton
            (
            string Text,
            Image? Icon,
            bool Odd,
            Action<object> OnClicked
            )
        {
            var panel = new ButtonPanel();
            panel.OnClicked += OnClicked;
            panel.Odd = Odd;
            panel.CaptionText = Text;
            panel.DisplayIcon = Icon;
            panel.Dock = DockStyle.Top;
            OptionsTable?.Controls.Add(panel);
        }

        private void CalibrationPage_Load(object sender, EventArgs e)
        {
            ShowOptions();
        }
    }
}
