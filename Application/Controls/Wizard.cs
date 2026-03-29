using OpenCvSharp.Internal.Vectors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AgGrade.Data;
using AgGrade.Controller;

namespace AgGrade.Controls
{
    public partial class Wizard : UserControl
    {
        public string Name
        {
            get { return WizardName.Text; }
            set { WizardName.Text = value; }
        }

        public event Action<object> ExitWizard = null;
        public EquipmentStatus? CurrentEquipmentStatus = null;
        public EquipmentSettings? CurrentEquipmentSettings = null;
        public OGController? Controller = null;

        private WizardControl? _Content;
        public WizardControl? Content
        {
            get { return _Content; }
            set
            {
                if (_Content != null)
                {
                    HideContent();
                }
                _Content = value;
                ShowContent();
            }
        }

        public Wizard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the wizard is being closed
        /// </summary>
        public void Closing
            (
            )
        {
            if (_Content != null)
            {
                _Content.Deactivated();
            }
        }

        /// <summary>
        /// Hides the wizard content
        /// </summary>
        private void HideContent
            (
            )
        {
            if (WizardBody.Controls.Contains(_Content))
            {
                WizardBody.Controls.Remove(_Content);
                _Content.ExitWizard -= _Content_ExitWizard;
            }
            _Content = null;
        }

        private void ShowContent
            (
            )
        {
            if (_Content != null)
            {
                _Content.HideTabs();
                _Content.Parent = WizardBody;
                _Content.Dock = DockStyle.Fill;
                _Content.ExitWizard += _Content_ExitWizard;
                _Content.CurrentEquipmentStatus = CurrentEquipmentStatus;
                _Content.CurrentEquipmentSettings = CurrentEquipmentSettings;
                _Content.Controller = Controller;
                _Content.Activated();
                WizardBody.Controls.Add(_Content);
            }
        }

        /// <summary>
        /// Called when wizard control requests the wizard exit
        /// </summary>
        /// <param name="obj"></param>
        private void _Content_ExitWizard(object obj)
        {
            _Content.Deactivated();
            ExitWizard?.Invoke(this);
        }

        /// <summary>
        /// Called when user taps on the back button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackBtn_Click(object sender, EventArgs e)
        {
            if (_Content != null)
            {
                int CurrentPage = _Content.CurrentPage;
                if (CurrentPage == 0)
                {
                    _Content.Deactivated();
                    ExitWizard?.Invoke(this);
                }
                else if (CurrentPage > 0)
                {
                    _Content.ShowPage(CurrentPage - 1);

                    // are we no longer showing the last page?
                    if (CurrentPage + 1 != _Content.NumberofPages - 1)
                    {
                        NextBtn.Enabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Called when the user taps on the next button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NextBtn_Click(object sender, EventArgs e)
        {
            if (_Content != null)
            {
                if (_Content.NumberofPages > 0)
                {
                    int CurrentPage = _Content.CurrentPage;
                    if (CurrentPage == _Content.NumberofPages - 1)
                    {

                    }
                    else if (CurrentPage != -1)
                    {
                        _Content.ShowPage(CurrentPage + 1);
                        
                        // are we now showing the last page?
                        if (CurrentPage + 1 == _Content.NumberofPages - 1)
                        {
                            NextBtn.Enabled = false;
                        }
                    }
                }
            }
        }
    }
}
