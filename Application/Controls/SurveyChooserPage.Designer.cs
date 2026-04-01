namespace AgGrade.Controls
{
    partial class SurveyChooserPage
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
            sectionTitle1 = new SectionTitle();
            SurveyTable = new Panel();
            SuspendLayout();
            // 
            // sectionTitle1
            // 
            sectionTitle1.Dock = DockStyle.Top;
            sectionTitle1.Location = new Point(0, 0);
            sectionTitle1.Name = "sectionTitle1";
            sectionTitle1.Size = new Size(916, 48);
            sectionTitle1.TabIndex = 0;
            sectionTitle1.TitleText = "Field Surveys";
            // 
            // SurveyTable
            // 
            SurveyTable.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            SurveyTable.AutoScroll = true;
            SurveyTable.Location = new Point(20, 64);
            SurveyTable.Name = "SurveyTable";
            SurveyTable.Size = new Size(873, 392);
            SurveyTable.TabIndex = 1;
            // 
            // SurveyChooserPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(SurveyTable);
            Controls.Add(sectionTitle1);
            Name = "SurveyChooserPage";
            Size = new Size(916, 476);
            ResumeLayout(false);
        }

        #endregion

        private SectionTitle sectionTitle1;
        private Panel SurveyTable;
    }
}
