namespace AgGrade
{
    partial class SplashForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            VersionLabel = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label1.Font = new Font("Segoe UI", 80F);
            label1.ForeColor = SystemColors.Control;
            label1.Location = new Point(0, 170);
            label1.Name = "label1";
            label1.Size = new Size(964, 182);
            label1.TabIndex = 0;
            label1.Text = "AgGrade";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // VersionLabel
            // 
            VersionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            VersionLabel.Font = new Font("Segoe UI", 30F);
            VersionLabel.ForeColor = SystemColors.Control;
            VersionLabel.Location = new Point(0, 334);
            VersionLabel.Name = "VersionLabel";
            VersionLabel.Size = new Size(964, 69);
            VersionLabel.TabIndex = 1;
            VersionLabel.Text = "Version X.X.X";
            VersionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SplashForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Green;
            ClientSize = new Size(964, 510);
            Controls.Add(VersionLabel);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SplashForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SplashForm";
            FormClosed += SplashForm_FormClosed;
            Shown += SplashForm_Shown;
            ResumeLayout(false);
        }

        #endregion

        private Label label1;
        private Label VersionLabel;
    }
}