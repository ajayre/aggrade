namespace AgGrade.Controls
{
    partial class SurveyPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            panel1 = new Panel();
            LastModified = new Label();
            SurveyName = new Label();
            Icon = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(LastModified);
            panel1.Controls.Add(SurveyName);
            panel1.Controls.Add(Icon);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(686, 64);
            panel1.TabIndex = 3;
            // 
            // LastModified
            // 
            LastModified.BackColor = Color.OldLace;
            LastModified.Dock = DockStyle.Fill;
            LastModified.Font = new Font("Segoe UI", 14F);
            LastModified.ImageAlign = ContentAlignment.MiddleLeft;
            LastModified.Location = new Point(374, 0);
            LastModified.Name = "LastModified";
            LastModified.Padding = new Padding(5, 0, 0, 0);
            LastModified.Size = new Size(312, 64);
            LastModified.TabIndex = 1;
            LastModified.Text = "Last Modified";
            LastModified.TextAlign = ContentAlignment.MiddleRight;
            LastModified.Click += LastModified_Click;
            // 
            // SurveyName
            // 
            SurveyName.BackColor = Color.OldLace;
            SurveyName.Dock = DockStyle.Left;
            SurveyName.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            SurveyName.Location = new Point(64, 0);
            SurveyName.Name = "SurveyName";
            SurveyName.Padding = new Padding(5, 0, 0, 0);
            SurveyName.Size = new Size(310, 64);
            SurveyName.TabIndex = 0;
            SurveyName.Text = "Survey Name";
            SurveyName.TextAlign = ContentAlignment.MiddleLeft;
            SurveyName.Click += FieldName_Click;
            // 
            // Icon
            // 
            Icon.BackColor = Color.OldLace;
            Icon.BackgroundImage = Properties.Resources.survey_48px;
            Icon.BackgroundImageLayout = ImageLayout.Center;
            Icon.Dock = DockStyle.Left;
            Icon.Location = new Point(0, 0);
            Icon.Name = "Icon";
            Icon.Size = new Size(64, 64);
            Icon.TabIndex = 2;
            Icon.Click += Icon_Click;
            // 
            // SurveyPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Name = "SurveyPanel";
            Size = new Size(686, 64);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Label SurveyName;
        private Label LastModified;
        private Panel Icon;
    }
}
