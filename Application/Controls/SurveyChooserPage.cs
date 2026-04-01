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
    public partial class SurveyChooserPage : UserControl
    {
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

        public event Action<string> OnSurveyChosen = null;
        public event Action OnCreateSurvey = null;

        public SurveyChooserPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Builds the UI to show the available surveys
        /// </summary>
        /// <param name="SurveyDataFolder">Path to folder that contains surveys</param>
        private void ShowSurveys(string SurveyDataFolder)
        {
            SurveyTable.Controls.Clear();

            if (string.IsNullOrEmpty(SurveyDataFolder) || !Directory.Exists(SurveyDataFolder))
                return;

            var entries = new List<(string FieldName, string FieldNameText, string LastModifiedText, DateTime SortDate, string? FileName)>();

            foreach (string SurveyFile in Directory.GetFiles(SurveyDataFolder, "*.txt"))
            {
                string SurveyName = Path.GetFileNameWithoutExtension(SurveyFile);

                FileInfo fi = new FileInfo(SurveyFile);

                // Create panel (sorts last within this field)
                entries.Add((SurveyFile, SurveyName, FormatLastModifiedFriendly(fi.LastWriteTime), fi.LastWriteTime, SurveyFile));
            }

            // Sort: survey name A–Z, then by date newest first with "Create New" last
            var sorted = entries
                .OrderBy(e => e.FieldName)
                .ThenByDescending(e => e.SortDate)
                .ToList();

            // Add in reverse order so first sorted item appears at top (DockStyle.Top stacks first-added at bottom)
            bool odd = (sorted.Count % 2) == 1; // so alternating stays correct when we add reverse
            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                var e = sorted[i];
                var panel = new SurveyPanel();
                panel.OnClicked += Panel_OnClicked;
                panel.Odd = odd;
                panel.SurveyNameText = e.FieldNameText;
                panel.LastModifiedText = e.LastModifiedText;
                panel.Dock = DockStyle.Top;
                panel.FileName = e.FileName;
                SurveyTable.Controls.Add(panel);
                odd = !odd;
            }

            // create spacer
            var spacerpanel = new Panel();
            spacerpanel.Height = 20;
            spacerpanel.Dock = DockStyle.Top;
            SurveyTable.Controls.Add(spacerpanel);

            // add entry to create a new survey
            var newpanel = new SurveyPanel();
            newpanel.OnClicked += Panel_OnClicked;
            newpanel.Odd = odd;
            newpanel.SurveyNameText = "Create New Survey";
            newpanel.LastModifiedText = "";
            newpanel.Dock = DockStyle.Top;
            newpanel.FileName = null;
            SurveyTable.Controls.Add(newpanel);
        }

        /// <summary>
        /// Called when a panel is tapped
        /// </summary>
        /// <param name="obj"></param>
        private void Panel_OnClicked(object sender)
        {
            SurveyPanel Panel = (SurveyPanel)sender;

            if (Panel.FileName == null)
            {
                OnCreateSurvey?.Invoke();
            }
            else
            {
                string SurveyFile = Panel.FileName;

                OnSurveyChosen?.Invoke(SurveyFile);
            }
        }

        /// <summary>
        /// Returns a user-friendly, Apple-style relative date/time string for the given time.
        /// </summary>
        private static string FormatLastModifiedFriendly(DateTime dt)
        {
            DateTime now = DateTime.Now;
            TimeSpan span = now - dt;

            if (span.TotalMinutes < 5)
                return "Just now";
            if (span.TotalHours < 1)
                return "In the last hour";
            if (span.TotalHours < 2)
                return "About an hour ago";
            if (dt.Date == now.Date)
                return "Today at " + dt.ToString("h:mm tt");
            if (dt.Date == now.Date.AddDays(-1))
                return "Yesterday at " + dt.ToString("h:mm tt");
            if (span.TotalDays >= 2 && span.TotalDays < 7)
                return dt.ToString("dddd") + " at " + dt.ToString("h:mm tt");
            if (span.TotalDays >= 7 && span.TotalDays < 14)
                return "Last " + dt.ToString("dddd");
            if (span.TotalDays >= 14 && span.TotalDays < 60)
                return (int)(span.TotalDays / 7) + " weeks ago";
            if (dt.Year == now.Year)
                return dt.ToString("MMM d");
            if (now.Year - dt.Year == 1)
                return "Last year";
            return dt.ToString("MMM d, yyyy");
        }
    }
}
