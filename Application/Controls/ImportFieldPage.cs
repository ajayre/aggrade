using AgGrade.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AgGrade.Controls
{
    public partial class ImportFieldPage : UserControl
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

        public ImportFieldPage()
        {
            InitializeComponent();

            ProgressOutput.ReadOnly = true;
            ProgressOutput.ScrollBars = ScrollBars.Vertical;

            ApplyDefaultFieldValues();
            ErrorMessage.Visible = false;
        }

        private void RunOnUiThread(Action action)
        {
            if (IsDisposed)
                return;
            try
            {
                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void AppendProgressLine(string line)
        {
            if (ProgressOutput.TextLength > 0)
                ProgressOutput.AppendText(Environment.NewLine);
            ProgressOutput.AppendText(line);
            ProgressOutput.SelectionStart = ProgressOutput.Text.Length;
            ProgressOutput.ScrollToCaret();
        }

        private void ApplyDefaultFieldValues()
        {
        }

        private bool ValidateFieldInputs()
        {
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
        /// Called when user taps on the button to import the field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportFieldBtn_Click(object sender, EventArgs e)
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
            };

            string? surveyDir = Path.GetDirectoryName(SurveyFile);
            string dbPath = Path.Combine(
                surveyDir ?? string.Empty,
                Path.GetFileNameWithoutExtension(SurveyFile) + "-base.db");

            ProgressOutput.Clear();
            ImportFieldBtn.Enabled = false;

            var worker = new Thread(() =>
            {
                try
                {
                    var fieldCreator = new FieldCreator(msg =>
                        RunOnUiThread(() => AppendProgressLine(msg)));
                    fieldCreator.CreateFromSurveyAndDesign(fieldDesign, dbPath);

                    RunOnUiThread(() => OnCreateField?.Invoke(fieldDesign));
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        ErrorMessage.Text = ex.Message;
                        ErrorMessage.Visible = true;
                    });
                }
                finally
                {
                    RunOnUiThread(() => ImportFieldBtn.Enabled = true);
                }
            })
            {
                IsBackground = true,
                Name = "ImportField"
            };
            worker.Start();
        }
    }
}
