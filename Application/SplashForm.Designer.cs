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
            VersionLabel = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // VersionLabel
            // 
            VersionLabel.Anchor = AnchorStyles.None;
            VersionLabel.Font = new Font("Segoe UI", 22F);
            VersionLabel.ForeColor = SystemColors.Control;
            VersionLabel.Location = new Point(4, 338);
            VersionLabel.Name = "VersionLabel";
            VersionLabel.Size = new Size(974, 69);
            VersionLabel.TabIndex = 1;
            VersionLabel.Text = "Version X.X.X";
            VersionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.None;
            pictureBox1.BackgroundImageLayout = ImageLayout.None;
            pictureBox1.Image = Properties.Resources.transparent_logo_darkgreen_400px1;
            pictureBox1.Location = new Point(294, 112);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(405, 223);
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            // 
            // SplashForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Green;
            ClientSize = new Size(982, 527);
            Controls.Add(pictureBox1);
            Controls.Add(VersionLabel);
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
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Label VersionLabel;
        private PictureBox pictureBox1;
    }
}