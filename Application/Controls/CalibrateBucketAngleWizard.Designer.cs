namespace AgGrade.Controls
{
    partial class CalibrateBucketAngleWizard
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
            Angle1 = new TextBox();
            textBox2 = new TextBox();
            CaptureZeroBtn = new Button();
            textBox1 = new TextBox();
            tabPage4 = new TabPage();
            ReturnBtn = new Button();
            ResultMsg = new TextBox();
            panel1 = new Panel();
            ErrorMessage = new Label();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            Pages.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage4.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // Pages
            // 
            Pages.Controls.Add(tabPage1);
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
            tabPage1.Controls.Add(Angle1);
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(CaptureZeroBtn);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(792, 434);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            // 
            // Angle1
            // 
            Angle1.BackColor = SystemColors.Control;
            Angle1.BorderStyle = BorderStyle.None;
            Angle1.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            Angle1.Location = new Point(260, 93);
            Angle1.Name = "Angle1";
            Angle1.ReadOnly = true;
            Angle1.Size = new Size(162, 29);
            Angle1.TabIndex = 27;
            Angle1.TabStop = false;
            Angle1.Text = "0 deg";
            // 
            // textBox2
            // 
            textBox2.BackColor = SystemColors.Control;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new Font("Segoe UI", 16F);
            textBox2.Location = new Point(6, 143);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.ReadOnly = true;
            textBox2.Size = new Size(385, 35);
            textBox2.TabIndex = 13;
            textBox2.TabStop = false;
            textBox2.Text = "4. Tap on Next";
            // 
            // CaptureZeroBtn
            // 
            CaptureZeroBtn.Font = new Font("Segoe UI", 18F);
            CaptureZeroBtn.Image = Properties.Resources.angle_48px;
            CaptureZeroBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CaptureZeroBtn.Location = new Point(35, 77);
            CaptureZeroBtn.Name = "CaptureZeroBtn";
            CaptureZeroBtn.Size = new Size(219, 60);
            CaptureZeroBtn.TabIndex = 12;
            CaptureZeroBtn.Text = "Capture Angle";
            CaptureZeroBtn.TextAlign = ContentAlignment.MiddleRight;
            CaptureZeroBtn.UseVisualStyleBackColor = true;
            CaptureZeroBtn.Click += CaptureZeroBtn_Click;
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
            textBox1.Size = new Size(497, 159);
            textBox1.TabIndex = 1;
            textBox1.TabStop = false;
            textBox1.Text = "1. Raise the apron (if equipped) but not the bucket\r\n2. Tap on button below";
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
            // CalibrateBucketAngleWizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Pages);
            Controls.Add(panel1);
            Name = "CalibrateBucketAngleWizard";
            Size = new Size(800, 506);
            Load += CalibrateBladeHeightWizard_Load;
            Pages.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
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
        private Button CaptureZeroBtn;
        private Panel panel1;
        private Label ErrorMessage;
        private TabPage tabPage4;
        private Button ReturnBtn;
        private TextBox ResultMsg;
        private TextBox Angle1;
        private System.Windows.Forms.Timer RefreshTimer;
    }
}
