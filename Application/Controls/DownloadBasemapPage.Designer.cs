namespace AgGrade.Controls
{
    partial class DownloadBasemapPage
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
            label1 = new Label();
            LatitudeInput = new TextBox();
            DownloadBtn = new Button();
            panel1 = new Panel();
            ProgressBar = new ProgressBar();
            ErrorMessage = new Label();
            LongitudeInput = new TextBox();
            label2 = new Label();
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
            sectionTitle1.TitleText = "Download Basemap";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 16F);
            label1.Location = new Point(37, 57);
            label1.Name = "label1";
            label1.Size = new Size(95, 30);
            label1.TabIndex = 1;
            label1.Text = "Latitude:";
            // 
            // LatitudeInput
            // 
            LatitudeInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            LatitudeInput.Font = new Font("Segoe UI", 16F);
            LatitudeInput.Location = new Point(138, 54);
            LatitudeInput.Name = "LatitudeInput";
            LatitudeInput.Size = new Size(775, 36);
            LatitudeInput.TabIndex = 2;
            // 
            // DownloadBtn
            // 
            DownloadBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            DownloadBtn.Font = new Font("Segoe UI", 18F);
            DownloadBtn.Image = Properties.Resources.download_48px;
            DownloadBtn.ImageAlign = ContentAlignment.MiddleLeft;
            DownloadBtn.Location = new Point(734, 140);
            DownloadBtn.Name = "DownloadBtn";
            DownloadBtn.Size = new Size(182, 60);
            DownloadBtn.TabIndex = 13;
            DownloadBtn.Text = "Download";
            DownloadBtn.TextAlign = ContentAlignment.MiddleRight;
            DownloadBtn.UseVisualStyleBackColor = true;
            DownloadBtn.Click += DownloadBtn_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(224, 224, 224);
            panel1.Controls.Add(ProgressBar);
            panel1.Controls.Add(ErrorMessage);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 432);
            panel1.Name = "panel1";
            panel1.Size = new Size(916, 44);
            panel1.TabIndex = 14;
            // 
            // ProgressBar
            // 
            ProgressBar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ProgressBar.Location = new Point(504, 8);
            ProgressBar.Name = "ProgressBar";
            ProgressBar.Size = new Size(403, 28);
            ProgressBar.TabIndex = 1;
            // 
            // ErrorMessage
            // 
            ErrorMessage.AutoSize = true;
            ErrorMessage.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            ErrorMessage.ForeColor = Color.Red;
            ErrorMessage.Location = new Point(10, 9);
            ErrorMessage.Name = "ErrorMessage";
            ErrorMessage.Size = new Size(139, 25);
            ErrorMessage.TabIndex = 0;
            ErrorMessage.Text = "Error Message";
            // 
            // LongitudeInput
            // 
            LongitudeInput.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            LongitudeInput.Font = new Font("Segoe UI", 16F);
            LongitudeInput.Location = new Point(138, 96);
            LongitudeInput.Name = "LongitudeInput";
            LongitudeInput.Size = new Size(775, 36);
            LongitudeInput.TabIndex = 15;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 16F);
            label2.Location = new Point(17, 99);
            label2.Name = "label2";
            label2.Size = new Size(115, 30);
            label2.TabIndex = 16;
            label2.Text = "Longitude:";
            // 
            // DownloadBasemapPage
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(label2);
            Controls.Add(LongitudeInput);
            Controls.Add(panel1);
            Controls.Add(DownloadBtn);
            Controls.Add(LatitudeInput);
            Controls.Add(label1);
            Controls.Add(sectionTitle1);
            Name = "DownloadBasemapPage";
            Size = new Size(916, 476);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private SectionTitle sectionTitle1;
        private Label label1;
        private TextBox LatitudeInput;
        private Button DownloadBtn;
        private Panel panel1;
        private Label ErrorMessage;
        private TextBox LongitudeInput;
        private Label label2;
        private ProgressBar ProgressBar;
    }
}
