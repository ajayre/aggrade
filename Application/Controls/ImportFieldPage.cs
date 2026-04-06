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

        private readonly List<string> _sourceFilePaths = new List<string>();

        private string _FieldDataFolder;
        public string FieldDataFolder
        {
            get { return _FieldDataFolder; }
            set
            {
                _FieldDataFolder = value;
                ShowSources(_FieldDataFolder);
            }
        }

        public ImportFieldPage()
        {
            InitializeComponent();

            ProgressOutput.ReadOnly = true;
            ProgressOutput.ScrollBars = ScrollBars.Vertical;

            ApplyDefaultFieldValues();
            ErrorMessage.Visible = false;

            HaulPaths.SelectedIndex = 0;
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
        /// Shows the currently available sources
        /// </summary>
        /// <param name="Folder">Path to folder containing field data</param>
        private void ShowSources
            (
            string Folder
            )
        {
            SourceChooser.Items.Clear();
            _sourceFilePaths.Clear();

            if (string.IsNullOrWhiteSpace(Folder) || !Directory.Exists(Folder))
            {
                SourceChooser.SelectedIndex = -1;
                ImportFieldBtn.Enabled = false;
                return;
            }

            ImportFieldBtn.Enabled = true;

            var paths = new List<string>();
            paths.AddRange(Directory.EnumerateFiles(Folder, "*.agd", SearchOption.AllDirectories));

            foreach (string path in paths.OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase))
            {
                _sourceFilePaths.Add(path);
                SourceChooser.Items.Add(Path.GetFileNameWithoutExtension(path));
            }

            SourceChooser.SelectedIndex = SourceChooser.Items.Count > 0 ? 0 : -1;

            if (SourceChooser.SelectedIndex == -1)
            {
                ImportFieldBtn.Enabled = false;
            }
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

            if (FieldDataFolder == null)
            {
                ErrorMessage.Text = "No field data folder";
                ErrorMessage.Visible = true;
                return;
            }

            if (SourceChooser.SelectedIndex < 0
                || SourceChooser.SelectedIndex >= _sourceFilePaths.Count)
            {
                ErrorMessage.Text = "No field source selected";
                ErrorMessage.Visible = true;
                return;
            }

            string SurveyFile = _sourceFilePaths[SourceChooser.SelectedIndex];

            if (!File.Exists(SurveyFile))
            {
                ErrorMessage.Text = "Field source doesn't exist";
                ErrorMessage.Visible = true;
                return;
            }

            bool GenerateHaulPaths = HaulPaths.SelectedIndex == 0 ? true : false;

            string? surveyDir = Path.GetDirectoryName(SurveyFile);
            string dbPath = Path.Combine(
                surveyDir ?? string.Empty,
                Path.GetFileNameWithoutExtension(SurveyFile) + ".db");

            ProgressOutput.Clear();
            ImportFieldBtn.Enabled = false;

            var worker = new Thread(() =>
            {
                try
                {
                    string importFolder = surveyDir ?? FieldDataFolder;
                    if (string.IsNullOrWhiteSpace(importFolder))
                        throw new InvalidOperationException("Could not determine import folder.");

                    var fieldImporter = new FieldImporter(msg =>
                        RunOnUiThread(() => AppendProgressLine(msg)));
                    fieldImporter.CreateFromAgd(
                        importFolder,
                        Path.GetFileName(SurveyFile),
                        Path.GetFileName(dbPath),
                        GenerateHaulPaths);
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
