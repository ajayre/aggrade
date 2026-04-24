namespace AgGrade.Controls
{
    partial class CalibrateIMUWizard
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
            components = new System.ComponentModel.Container();
            Pages = new TabControl();
            tabPage1 = new TabPage();
            OrientationSelector = new ComboBox();
            OrientationImage = new PictureBox();
            textBox2 = new TextBox();
            textBox1 = new TextBox();
            tabPage2 = new TabPage();
            CaptureZeroBtn = new Button();
            tabPage4 = new TabPage();
            ReturnBtn = new Button();
            ResultMsg = new TextBox();
            panel1 = new Panel();
            ErrorMessage = new Label();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            Pages.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)OrientationImage).BeginInit();
            tabPage2.SuspendLayout();
            tabPage4.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // Pages
            // 
            Pages.Controls.Add(tabPage1);
            Pages.Controls.Add(tabPage2);
            Pages.Controls.Add(tabPage4);
            Pages.Dock = DockStyle.Fill;
            Pages.Location = new Point(0, 0);
            Pages.Name = "Pages";
            Pages.SelectedIndex = 0;
            Pages.Size = new Size(800, 462);
            Pages.TabIndex = 0;
            Pages.SelectedIndexChanged += Pages_SelectedIndexChanged;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(OrientationSelector);
            tabPage1.Controls.Add(OrientationImage);
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(792, 434);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            // 
            // OrientationSelector
            // 
            OrientationSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            OrientationSelector.Font = new Font("Segoe UI", 16F);
            OrientationSelector.FormattingEnabled = true;
            OrientationSelector.Items.AddRange(new object[] { "Horizontal", "Vertical" });
            OrientationSelector.Location = new Point(33, 81);
            OrientationSelector.Name = "OrientationSelector";
            OrientationSelector.Size = new Size(279, 38);
            OrientationSelector.TabIndex = 29;
            OrientationSelector.SelectedIndexChanged += OrientationSelector_SelectedIndexChanged;
            // 
            // OrientationImage
            // 
            OrientationImage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            OrientationImage.BackgroundImage = Properties.Resources.IMU_Vertical;
            OrientationImage.BackgroundImageLayout = ImageLayout.Center;
            OrientationImage.Location = new Point(476, 6);
            OrientationImage.Name = "OrientationImage";
            OrientationImage.Size = new Size(310, 310);
            OrientationImage.TabIndex = 28;
            OrientationImage.TabStop = false;
            // 
            // textBox2
            // 
            textBox2.BackColor = SystemColors.Control;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new Font("Segoe UI", 16F);
            textBox2.Location = new Point(6, 128);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.ReadOnly = true;
            textBox2.Size = new Size(385, 35);
            textBox2.TabIndex = 13;
            textBox2.TabStop = false;
            textBox2.Text = "3. Tap on Next";
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.BackColor = SystemColors.Control;
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Font = new Font("Segoe UI", 16F);
            textBox1.Location = new Point(6, 6);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new Size(457, 159);
            textBox1.TabIndex = 1;
            textBox1.TabStop = false;
            textBox1.Text = "1. Drive to a level location\r\n2. Choose IMU orientation";
            // 
            // tabPage2
            // 
            tabPage2.BackColor = SystemColors.Control;
            tabPage2.Controls.Add(CaptureZeroBtn);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 434);
            tabPage2.TabIndex = 4;
            tabPage2.Text = "tabPage2";
            // 
            // CaptureZeroBtn
            // 
            CaptureZeroBtn.Font = new Font("Segoe UI", 18F);
            CaptureZeroBtn.Image = Properties.Resources.angle_48px;
            CaptureZeroBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CaptureZeroBtn.Location = new Point(287, 187);
            CaptureZeroBtn.Name = "CaptureZeroBtn";
            CaptureZeroBtn.Size = new Size(219, 60);
            CaptureZeroBtn.TabIndex = 13;
            CaptureZeroBtn.Text = "Capture Angle";
            CaptureZeroBtn.TextAlign = ContentAlignment.MiddleRight;
            CaptureZeroBtn.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            tabPage4.BackColor = SystemColors.Control;
            tabPage4.Controls.Add(ReturnBtn);
            tabPage4.Controls.Add(ResultMsg);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(792, 434);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "tabPage4";
            // 
            // ReturnBtn
            // 
            ReturnBtn.Font = new Font("Segoe UI", 18F);
            ReturnBtn.Image = Properties.Resources.calibration_48px;
            ReturnBtn.ImageAlign = ContentAlignment.MiddleLeft;
            ReturnBtn.Location = new Point(6, 46);
            ReturnBtn.Name = "ReturnBtn";
            ReturnBtn.Size = new Size(339, 60);
            ReturnBtn.TabIndex = 21;
            ReturnBtn.Text = "Return To Calibration List";
            ReturnBtn.TextAlign = ContentAlignment.MiddleRight;
            ReturnBtn.UseVisualStyleBackColor = true;
            ReturnBtn.Click += ReturnBtn_Click;
            // 
            // ResultMsg
            // 
            ResultMsg.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ResultMsg.BackColor = SystemColors.Control;
            ResultMsg.BorderStyle = BorderStyle.None;
            ResultMsg.Font = new Font("Segoe UI", 16F);
            ResultMsg.Location = new Point(6, 6);
            ResultMsg.Multiline = true;
            ResultMsg.Name = "ResultMsg";
            ResultMsg.ReadOnly = true;
            ResultMsg.Size = new Size(780, 35);
            ResultMsg.TabIndex = 20;
            ResultMsg.TabStop = false;
            ResultMsg.Text = "Result message\r\n";
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(224, 224, 224);
            panel1.Controls.Add(ErrorMessage);
            panel1.Dock = DockStyle.Bottom;
            panel1.Location = new Point(0, 462);
            panel1.Name = "panel1";
            panel1.Size = new Size(800, 44);
            panel1.TabIndex = 1;
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
            ErrorMessage.VisibleChanged += ErrorMessage_VisibleChanged;
            // 
            // RefreshTimer
            // 
            RefreshTimer.Interval = 250;
            RefreshTimer.Tick += RefreshTimer_Tick;
            // 
            // CalibrateIMUWizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Pages);
            Controls.Add(panel1);
            Name = "CalibrateIMUWizard";
            Size = new Size(800, 506);
            Load += CalibrateBladeHeightWizard_Load;
            Pages.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)OrientationImage).EndInit();
            tabPage2.ResumeLayout(false);
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl Pages;
        private TabPage tabPage1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Panel panel1;
        private Label ErrorMessage;
        private TabPage tabPage4;
        private Button ReturnBtn;
        private TextBox ResultMsg;
        private System.Windows.Forms.Timer RefreshTimer;
        private PictureBox OrientationImage;
        private ComboBox OrientationSelector;
        private TabPage tabPage2;
        private Button CaptureZeroBtn;
    }
}
