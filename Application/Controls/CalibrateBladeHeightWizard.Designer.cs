namespace AgGrade.Controls
{
    partial class CalibrateBladeHeightWizard
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
            Pages = new TabControl();
            tabPage1 = new TabPage();
            textBox2 = new TextBox();
            CaptureZeroBtn = new Button();
            textBox1 = new TextBox();
            pictureBox1 = new PictureBox();
            tabPage2 = new TabPage();
            button1 = new Button();
            pictureBox2 = new PictureBox();
            textBox8 = new TextBox();
            PageTwoInstructions = new TextBox();
            tabPage3 = new TabPage();
            pictureBox3 = new PictureBox();
            textBox5 = new TextBox();
            button2 = new Button();
            textBox4 = new TextBox();
            tabPage4 = new TabPage();
            ReturnBtn = new Button();
            ResultMsg = new TextBox();
            panel1 = new Panel();
            ErrorMessage = new Label();
            Pages.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            tabPage4.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // Pages
            // 
            Pages.Controls.Add(tabPage1);
            Pages.Controls.Add(tabPage2);
            Pages.Controls.Add(tabPage3);
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
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(CaptureZeroBtn);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Controls.Add(pictureBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(792, 434);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            // 
            // textBox2
            // 
            textBox2.BackColor = SystemColors.Control;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new Font("Segoe UI", 16F);
            textBox2.Location = new Point(6, 203);
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
            CaptureZeroBtn.Image = Properties.Resources.height_48px;
            CaptureZeroBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CaptureZeroBtn.Location = new Point(35, 137);
            CaptureZeroBtn.Name = "CaptureZeroBtn";
            CaptureZeroBtn.Size = new Size(228, 60);
            CaptureZeroBtn.TabIndex = 12;
            CaptureZeroBtn.Text = "Capture Height";
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
            textBox1.Size = new Size(457, 159);
            textBox1.TabIndex = 1;
            textBox1.TabStop = false;
            textBox1.Text = "1. Place the pan on a hard, level surface\r\n2. Move the joystick up or down until the blade is just touching the surface\r\n3. Tap on button below";
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox1.Image = Properties.Resources.joystick_jog;
            pictureBox1.Location = new Point(668, 6);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(118, 204);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(button1);
            tabPage2.Controls.Add(pictureBox2);
            tabPage2.Controls.Add(textBox8);
            tabPage2.Controls.Add(PageTwoInstructions);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 434);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            // 
            // button1
            // 
            button1.Font = new Font("Segoe UI", 18F);
            button1.Image = Properties.Resources.height_48px;
            button1.ImageAlign = ContentAlignment.MiddleLeft;
            button1.Location = new Point(33, 81);
            button1.Name = "button1";
            button1.Size = new Size(228, 60);
            button1.TabIndex = 22;
            button1.Text = "Capture Height";
            button1.TextAlign = ContentAlignment.MiddleRight;
            button1.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            pictureBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox2.Image = Properties.Resources.joystick_down;
            pictureBox2.Location = new Point(672, 44);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(115, 160);
            pictureBox2.TabIndex = 21;
            pictureBox2.TabStop = false;
            // 
            // textBox8
            // 
            textBox8.BackColor = SystemColors.Control;
            textBox8.BorderStyle = BorderStyle.None;
            textBox8.Font = new Font("Segoe UI", 16F);
            textBox8.Location = new Point(6, 150);
            textBox8.Multiline = true;
            textBox8.Name = "textBox8";
            textBox8.ReadOnly = true;
            textBox8.Size = new Size(385, 35);
            textBox8.TabIndex = 20;
            textBox8.TabStop = false;
            textBox8.Text = "3. Tap on Next";
            // 
            // PageTwoInstructions
            // 
            PageTwoInstructions.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            PageTwoInstructions.BackColor = SystemColors.Control;
            PageTwoInstructions.BorderStyle = BorderStyle.None;
            PageTwoInstructions.Font = new Font("Segoe UI", 16F);
            PageTwoInstructions.Location = new Point(6, 9);
            PageTwoInstructions.Multiline = true;
            PageTwoInstructions.Name = "PageTwoInstructions";
            PageTwoInstructions.ReadOnly = true;
            PageTwoInstructions.Size = new Size(437, 68);
            PageTwoInstructions.TabIndex = 18;
            PageTwoInstructions.TabStop = false;
            PageTwoInstructions.Text = "1. Move joystick down as far as blade will go\r\n2. Tap on button below\r\n";
            // 
            // tabPage3
            // 
            tabPage3.BackColor = SystemColors.Control;
            tabPage3.Controls.Add(pictureBox3);
            tabPage3.Controls.Add(textBox5);
            tabPage3.Controls.Add(button2);
            tabPage3.Controls.Add(textBox4);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(792, 434);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "tabPage3";
            // 
            // pictureBox3
            // 
            pictureBox3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox3.Image = Properties.Resources.joystick_up;
            pictureBox3.Location = new Point(670, 10);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(114, 199);
            pictureBox3.TabIndex = 25;
            pictureBox3.TabStop = false;
            // 
            // textBox5
            // 
            textBox5.BackColor = SystemColors.Control;
            textBox5.BorderStyle = BorderStyle.None;
            textBox5.Font = new Font("Segoe UI", 16F);
            textBox5.Location = new Point(6, 150);
            textBox5.Multiline = true;
            textBox5.Name = "textBox5";
            textBox5.ReadOnly = true;
            textBox5.Size = new Size(385, 35);
            textBox5.TabIndex = 24;
            textBox5.TabStop = false;
            textBox5.Text = "3. Tap on Next";
            // 
            // button2
            // 
            button2.Font = new Font("Segoe UI", 18F);
            button2.Image = Properties.Resources.height_48px;
            button2.ImageAlign = ContentAlignment.MiddleLeft;
            button2.Location = new Point(33, 81);
            button2.Name = "button2";
            button2.Size = new Size(228, 60);
            button2.TabIndex = 23;
            button2.Text = "Capture Height";
            button2.TextAlign = ContentAlignment.MiddleRight;
            button2.UseVisualStyleBackColor = true;
            // 
            // textBox4
            // 
            textBox4.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox4.BackColor = SystemColors.Control;
            textBox4.BorderStyle = BorderStyle.None;
            textBox4.Font = new Font("Segoe UI", 16F);
            textBox4.Location = new Point(6, 9);
            textBox4.Multiline = true;
            textBox4.Name = "textBox4";
            textBox4.ReadOnly = true;
            textBox4.Size = new Size(444, 70);
            textBox4.TabIndex = 19;
            textBox4.TabStop = false;
            textBox4.Text = "1. Move joystick up as far as blade will go\r\n2. Tap on button below\r\n";
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
            // CalibrateBladeHeightWizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Pages);
            Controls.Add(panel1);
            Name = "CalibrateBladeHeightWizard";
            Size = new Size(800, 506);
            Load += CalibrateBladeHeightWizard_Load;
            Pages.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl Pages;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private PictureBox pictureBox1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Button CaptureZeroBtn;
        private TextBox PageTwoInstructions;
        private TextBox textBox8;
        private TabPage tabPage3;
        private Panel panel1;
        private Label ErrorMessage;
        private PictureBox pictureBox2;
        private Button button1;
        private TabPage tabPage4;
        private Button ReturnBtn;
        private TextBox ResultMsg;
        private TextBox textBox4;
        private TextBox textBox5;
        private Button button2;
        private PictureBox pictureBox3;
    }
}
