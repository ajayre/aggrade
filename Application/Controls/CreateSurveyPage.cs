using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class CreateSurveyPage : UserControl
    {
        public event Action<string> OnSurveyCreated = null;

        public string? SurveyDataFolder = null;

        public CreateSurveyPage()
        {
            InitializeComponent();

            ErrorMessage.Visible = false;
            FileTypeChooser.SelectedIndex = 0;
        }

        /// <summary>
        /// Called when user taps on the button to create the survey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateSurveyBtn_Click(object sender, EventArgs e)
        {
            if (SurveyDataFolder == null)
            {
                ErrorMessage.Text = "No survey data folder";
                ErrorMessage.Visible = true;
                return;
            }

            string Name = NameInput.Text.Trim();
            if (Name.Length == 0)
            {
                ErrorMessage.Text = "No name given";
                ErrorMessage.Visible = true;
                return;
            }

            string Ext = string.Empty;
            switch (FileTypeChooser.SelectedIndex)
            {
                case 0: Ext = ".txt"; break;
                case 1: Ext = ".ags"; break;
            }

            string FileName = SurveyDataFolder + Name + Ext;

            if (File.Exists(FileName))
            {
                ErrorMessage.Text = "Survey already exists";
                ErrorMessage.Visible = true;
                return;
            }

            File.Create(FileName).Dispose();

            OnSurveyCreated?.Invoke(FileName);
        }
    }
}
