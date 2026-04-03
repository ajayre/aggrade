namespace AgGrade.Controls
{
    partial class FieldChooserPage
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
            FieldTable = new Panel();
            panel1 = new Panel();
            ProgressBar = new ProgressBar();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // sectionTitle1
            // 
            sectionTitle1.Dock = DockStyle.Top;
            sectionTitle1.Location = new Point(0, 0);
            sectionTitle1.Name = "sectionTitle1";
            sectionTitle1.Size = new Size(916, 48);
            sectionTitle1.TabIndex = 0;
            sectionTitle1.TitleText = "Load Field";
            // 
            // FieldTable
            // 
            FieldTable.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            FieldTable.AutoScroll = true;
            FieldTable.Location = new Point(20, 64);
            FieldTable.Name = "FieldTable";
            FieldTable.Size = new Size(873, 404);
            FieldTable.TabIndex = 1;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(224, 224, 224);
            panel1.Controls.Add(ProgressBar);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 474);
            panel1.Name = "panel1";
            panel1.Size = new Size(916, 44);
            panel1.TabIndex = 2;
            // 
            // ProgressBar
            // 
            ProgressBar.Location = new Point(9, 8);
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(403, 28);
            ProgressBar.TabIndex = 0;
            // 
            // FieldChooserPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Controls.Add(FieldTable);
            Controls.Add(sectionTitle1);
            Name = "FieldChooserPage";
            Size = new Size(916, 518);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SectionTitle sectionTitle1;
        private Panel FieldTable;
        private Panel panel1;
        private ProgressBar ProgressBar;
    }
}
