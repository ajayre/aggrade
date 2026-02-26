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
    public partial class FieldChooserPage : UserControl
    {
        private string _FieldDataFolder;
        public string FieldDataFolder
        {
            get { return _FieldDataFolder; }
            set
            {
                _FieldDataFolder = value;
                ShowFields(_FieldDataFolder);
            }
        }

        public FieldChooserPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Builds the UI to show the available fields
        /// </summary>
        /// <param name="FieldDataFolder">Path to folder that contains fields</param>
        private void ShowFields(string FieldDataFolder)
        {
            FieldTable.Controls.Clear();

            if (string.IsNullOrEmpty(FieldDataFolder) || !Directory.Exists(FieldDataFolder))
                return;

            var entries = new List<(string FieldName, string FieldNameText, string LastModifiedText, DateTime SortDate)>();

            foreach (string subDir in Directory.GetDirectories(FieldDataFolder))
            {
                string fieldName = Path.GetFileName(subDir);
                if (string.IsNullOrEmpty(fieldName))
                    continue;

                if (Directory.GetFiles(subDir, "*.agd").Length == 0)
                    continue;

                // Create New panel (sorts last within this field)
                entries.Add((fieldName, fieldName, "Create New", DateTime.MinValue));

                // One panel per .db file, newest first for sorting
                var dbFiles = Directory.GetFiles(subDir, "*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(fi => fi.LastWriteTimeUtc);
                foreach (var fi in dbFiles)
                {
                    string dbFileName = Path.GetFileNameWithoutExtension(fi.FullName);
                    entries.Add((
                        fieldName,
                        fieldName + " (" + dbFileName + ")",
                        FormatLastModifiedFriendly(fi.LastWriteTime),
                        fi.LastWriteTime
                    ));
                }
            }

            // Sort: field name A–Z, then by date newest first with "Create New" last
            var sorted = entries
                .OrderBy(e => e.FieldName)
                .ThenByDescending(e => e.SortDate)
                .ToList();

            // Add in reverse order so first sorted item appears at top (DockStyle.Top stacks first-added at bottom)
            bool odd = (sorted.Count % 2) == 1; // so alternating stays correct when we add reverse
            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                var e = sorted[i];
                var panel = new FieldPanel();
                panel.Odd = odd;
                panel.FieldNameText = e.FieldNameText;
                panel.LastModifiedText = e.LastModifiedText;
                panel.Dock = DockStyle.Top;
                FieldTable.Controls.Add(panel);
                odd = !odd;
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
