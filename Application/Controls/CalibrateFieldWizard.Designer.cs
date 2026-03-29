namespace AgGrade.Controls
{
    partial class CalibrateFieldWizard
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
            BMOffset = new TextBox();
            textBox2 = new TextBox();
            CaptureLocationBtn = new Button();
            textBox1 = new TextBox();
            tabPage2 = new TabPage();
            ReturnBtn = new Button();
            ResultMsg = new TextBox();
            panel1 = new Panel();
            ErrorMessage = new Label();
            RefreshTimer = new System.Windows.Forms.Timer(components);
            Pages.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // Pages
            // 
            Pages.Controls.Add(tabPage1);
            Pages.Controls.Add(tabPage2);
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
            tabPage1.Controls.Add(BMOffset);
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(CaptureLocationBtn);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(792, 434);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            // 
            // BMOffset
            // 
            BMOffset.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            BMOffset.BackColor = SystemColors.Control;
            BMOffset.BorderStyle = BorderStyle.None;
            BMOffset.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            BMOffset.Location = new Point(34, 203);
            BMOffset.Name = "BMOffset";
            BMOffset.ReadOnly = true;
            BMOffset.Size = new Size(752, 29);
            BMOffset.TabIndex = 27;
            BMOffset.TabStop = false;
            BMOffset.Text = "E = 0 mm, N = 0mm, H = 0mm";
            // 
            // textBox2
            // 
            textBox2.BackColor = SystemColors.Control;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new Font("Segoe UI", 16F);
            textBox2.Location = new Point(3, 238);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.ReadOnly = true;
            textBox2.Size = new Size(385, 35);
            textBox2.TabIndex = 13;
            textBox2.TabStop = false;
            textBox2.Text = "3. Tap on Next";
            // 
            // CaptureLocationBtn
            // 
            CaptureLocationBtn.Font = new Font("Segoe UI", 18F);
            CaptureLocationBtn.Image = Properties.Resources.location_48px;
            CaptureLocationBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CaptureLocationBtn.Location = new Point(34, 137);
            CaptureLocationBtn.Name = "CaptureLocationBtn";
            CaptureLocationBtn.Size = new Size(251, 60);
            CaptureLocationBtn.TabIndex = 12;
            CaptureLocationBtn.Text = "Capture Location";
            CaptureLocationBtn.TextAlign = ContentAlignment.MiddleRight;
            CaptureLocationBtn.UseVisualStyleBackColor = true;
            CaptureLocationBtn.Click += CaptureLocationBtn_Click;
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
            textBox1.Text = "1. Use the map to place a benchmark under the center of the rear axle\r\nHint: Switch to dot mode and zoom in\r\n2. Tap on button below";
            // 
            // tabPage2
            // 
            tabPage2.BackColor = SystemColors.Control;
            tabPage2.Controls.Add(ReturnBtn);
            tabPage2.Controls.Add(ResultMsg);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 434);
            tabPage2.TabIndex = 3;
            tabPage2.Text = "tabPage2";
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
            // CalibrateFieldWizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Pages);
            Controls.Add(panel1);
            Name = "CalibrateFieldWizard";
            Size = new Size(800, 506);
            Load += CalibrateFieldWizard_Load;
            Pages.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl Pages;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TextBox textBox1;
        private TextBox textBox2;
        private Button CaptureLocationBtn;
        private Panel panel1;
        private Label ErrorMessage;
        private Button ReturnBtn;
        private TextBox ResultMsg;
        private TextBox BMOffset;
        private System.Windows.Forms.Timer RefreshTimer;
    }
}
