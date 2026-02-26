namespace AgGrade.Controls
{
    partial class FieldPanel
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
            FieldName = new Label();
            LastModified = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(LastModified);
            panel1.Controls.Add(FieldName);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(686, 64);
            panel1.TabIndex = 3;
            // 
            // FieldName
            // 
            FieldName.BackColor = Color.OldLace;
            FieldName.Dock = DockStyle.Left;
            FieldName.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            FieldName.Location = new Point(0, 0);
            FieldName.Name = "FieldName";
            FieldName.Padding = new Padding(5, 0, 0, 0);
            FieldName.Size = new Size(398, 64);
            FieldName.TabIndex = 0;
            FieldName.Text = "Field Name";
            FieldName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LastModified
            // 
            LastModified.BackColor = Color.OldLace;
            LastModified.Dock = DockStyle.Fill;
            LastModified.Font = new Font("Segoe UI", 14F);
            LastModified.Location = new Point(398, 0);
            LastModified.Name = "LastModified";
            LastModified.Padding = new Padding(5, 0, 0, 0);
            LastModified.Size = new Size(288, 64);
            LastModified.TabIndex = 1;
            LastModified.Text = "Last Modified";
            LastModified.TextAlign = ContentAlignment.MiddleRight;
            // 
            // FieldPanel
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Name = "FieldPanel";
            Size = new Size(686, 64);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private Label FieldName;
        private Label LastModified;
    }
}
