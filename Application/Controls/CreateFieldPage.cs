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
    public partial class CreateFieldPage : UserControl
    {
        public event Action<FieldDesign> OnCreateField = null;

        private readonly List<string> _surveyFilePaths = new List<string>();

        private string _SurveyDataFolder;
        public string SurveyDataFolder
        {
            get { return _SurveyDataFolder; }
            set
            {
                _SurveyDataFolder = value;
                ShowSurveys(_SurveyDataFolder);
            }
        }

        public CreateFieldPage()
        {
            InitializeComponent();

            ApplyDefaultFieldValues();
            ErrorMessage.Visible = false;
        }

        private void ApplyDefaultFieldValues()
        {
            MainSlopeDirection.Unsigned = true;
            MainSlopeDirection.Value = 0;

            MainSlope.Minimum = 0;
            MainSlope.Maximum = 20;
            MainSlope.DecimalPlaces = 1;
            MainSlope.ButtonChangeAmount = 0.1;
            MainSlope.Value = 0.1;

            CrossSlope.Minimum = 0;
            CrossSlope.Maximum = 20;
            CrossSlope.DecimalPlaces = 1;
            CrossSlope.ButtonChangeAmount = 0.1;
            CrossSlope.Value = 0;

            CutFillRatio.Minimum = 0.5;
            CutFillRatio.DecimalPlaces = 2;
            CutFillRatio.ButtonChangeAmount = 0.1;
            CutFillRatio.Value = 1.2;

            ImportToField.Value = 0;
            ImportToField.Unsigned = true;
            ExportFromField.Value = 0;
            ExportFromField.Unsigned = true;
        }

        private bool ValidateFieldInputs()
        {
            if (MainSlopeDirection.Value < 0 || MainSlopeDirection.Value > 359)
            {
                ErrorMessage.Text = "Main slope direction must be 0 to 359 degrees";
                ErrorMessage.Visible = true;
                return false;
            }

            if (MainSlope.Value < 0 || MainSlope.Value > 20)
            {
                ErrorMessage.Text = "Main slope must be 0 to 20%";
                ErrorMessage.Visible = true;
                return false;
            }

            if (CrossSlope.Value < 0 || CrossSlope.Value > 20)
            {
                ErrorMessage.Text = "Cross slope must be 0 to 20%";
                ErrorMessage.Visible = true;
                return false;
            }

            if (CutFillRatio.Value < 0.5)
            {
                ErrorMessage.Text = "Cut/fill ratio must be 0.5 or greater";
                ErrorMessage.Visible = true;
                return false;
            }

            if (ImportToField.Value < 0)
            {
                ErrorMessage.Text = "Import to field must be 0 or greater";
                ErrorMessage.Visible = true;
                return false;
            }

            if (ExportFromField.Value < 0)
            {
                ErrorMessage.Text = "Export from field must be 0 or greater";
                ErrorMessage.Visible = true;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shows the currently available surveys
        /// </summary>
        /// <param name="Folder">Path to folder containing survey data</param>
        private void ShowSurveys
            (
            string Folder
            )
        {
            SurveyChooser.Items.Clear();
            _surveyFilePaths.Clear();

            if (string.IsNullOrWhiteSpace(Folder) || !Directory.Exists(Folder))
            {
                SurveyChooser.SelectedIndex = -1;
                return;
            }

            var paths = new List<string>();
            paths.AddRange(Directory.EnumerateFiles(Folder, "*.txt", SearchOption.TopDirectoryOnly));
            paths.AddRange(Directory.EnumerateFiles(Folder, "*.ags", SearchOption.TopDirectoryOnly));

            foreach (string path in paths.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            {
                _surveyFilePaths.Add(path);
                SurveyChooser.Items.Add(Path.GetFileNameWithoutExtension(path));
            }

            SurveyChooser.SelectedIndex = SurveyChooser.Items.Count > 0 ? 0 : -1;
        }

        /// <summary>
        /// Called when user taps on the button to create the field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateFieldBtn_Click(object sender, EventArgs e)
        {
            ErrorMessage.Visible = false;

            if (!ValidateFieldInputs())
            {
                return;
            }

            if (SurveyDataFolder == null)
            {
                ErrorMessage.Text = "No survey data folder";
                ErrorMessage.Visible = true;
                return;
            }

            if (SurveyChooser.SelectedIndex < 0
                || SurveyChooser.SelectedIndex >= _surveyFilePaths.Count)
            {
                ErrorMessage.Text = "No survey selected";
                ErrorMessage.Visible = true;
                return;
            }

            string SurveyFile = _surveyFilePaths[SurveyChooser.SelectedIndex];

            if (!File.Exists(SurveyFile))
            {
                ErrorMessage.Text = "Survey doesn't exist";
                ErrorMessage.Visible = true;
                return;
            }

            FieldDesign fieldDesign = new FieldDesign
            {
                SurveyFileName = SurveyFile,
                MainSlopeDirection = MainSlopeDirection.Value,
                MainSlope = MainSlope.Value,
                CrossSlope = CrossSlope.Value,
                CutFillRatio = CutFillRatio.Value,
                ImportToField = ImportToField.Value,
                ExportFromField = ExportFromField.Value
            };

            OnCreateField?.Invoke(fieldDesign);
        }
    }
}
