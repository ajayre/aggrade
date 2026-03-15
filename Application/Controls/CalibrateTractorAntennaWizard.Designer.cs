namespace AgGrade.Controls
{
    partial class CalibrateTractorAntennaWizard
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
            textBox7 = new TextBox();
            textBox6 = new TextBox();
            textBox5 = new TextBox();
            textBox4 = new TextBox();
            textBox3 = new TextBox();
            DInput = new NumericInput();
            TInput = new NumericInput();
            textBox2 = new TextBox();
            CapturePose1Btn = new Button();
            textBox1 = new TextBox();
            pictureBox1 = new PictureBox();
            tabPage2 = new TabPage();
            textBox8 = new TextBox();
            CapturePose2Btn = new Button();
            textBox9 = new TextBox();
            pictureBox2 = new PictureBox();
            tabPage3 = new TabPage();
            ReturnBtn = new Button();
            textBox10 = new TextBox();
            Pages.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            tabPage3.SuspendLayout();
            SuspendLayout();
            // 
            // Pages
            // 
            Pages.Controls.Add(tabPage1);
            Pages.Controls.Add(tabPage2);
            Pages.Controls.Add(tabPage3);
            Pages.Dock = DockStyle.Fill;
            Pages.Location = new Point(0, 0);
            Pages.Name = "Pages";
            Pages.SelectedIndex = 0;
            Pages.Size = new Size(800, 448);
            Pages.TabIndex = 0;
            Pages.SelectedIndexChanged += Pages_SelectedIndexChanged;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(textBox7);
            tabPage1.Controls.Add(textBox6);
            tabPage1.Controls.Add(textBox5);
            tabPage1.Controls.Add(textBox4);
            tabPage1.Controls.Add(textBox3);
            tabPage1.Controls.Add(DInput);
            tabPage1.Controls.Add(TInput);
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(CapturePose1Btn);
            tabPage1.Controls.Add(textBox1);
            tabPage1.Controls.Add(pictureBox1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(792, 420);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "tabPage1";
            // 
            // textBox7
            // 
            textBox7.BackColor = SystemColors.Control;
            textBox7.BorderStyle = BorderStyle.None;
            textBox7.Font = new Font("Segoe UI", 16F);
            textBox7.Location = new Point(30, 191);
            textBox7.Name = "textBox7";
            textBox7.ReadOnly = true;
            textBox7.Size = new Size(26, 29);
            textBox7.TabIndex = 20;
            textBox7.Text = "D:";
            // 
            // textBox6
            // 
            textBox6.BackColor = SystemColors.Control;
            textBox6.BorderStyle = BorderStyle.None;
            textBox6.Font = new Font("Segoe UI", 16F);
            textBox6.Location = new Point(233, 191);
            textBox6.Name = "textBox6";
            textBox6.ReadOnly = true;
            textBox6.Size = new Size(46, 29);
            textBox6.TabIndex = 19;
            textBox6.Text = "mm";
            // 
            // textBox5
            // 
            textBox5.BackColor = SystemColors.Control;
            textBox5.BorderStyle = BorderStyle.None;
            textBox5.Font = new Font("Segoe UI", 16F);
            textBox5.Location = new Point(233, 143);
            textBox5.Name = "textBox5";
            textBox5.ReadOnly = true;
            textBox5.Size = new Size(46, 29);
            textBox5.TabIndex = 18;
            textBox5.Text = "mm";
            // 
            // textBox4
            // 
            textBox4.BackColor = SystemColors.Control;
            textBox4.BorderStyle = BorderStyle.None;
            textBox4.Font = new Font("Segoe UI", 16F);
            textBox4.Location = new Point(33, 143);
            textBox4.Name = "textBox4";
            textBox4.ReadOnly = true;
            textBox4.Size = new Size(26, 29);
            textBox4.TabIndex = 17;
            textBox4.Text = "T:";
            // 
            // textBox3
            // 
            textBox3.BackColor = SystemColors.Control;
            textBox3.BorderStyle = BorderStyle.None;
            textBox3.Font = new Font("Segoe UI", 16F);
            textBox3.Location = new Point(6, 238);
            textBox3.Multiline = true;
            textBox3.Name = "textBox3";
            textBox3.ReadOnly = true;
            textBox3.Size = new Size(385, 35);
            textBox3.TabIndex = 16;
            textBox3.TabStop = false;
            textBox3.Text = "4. Tap on Button Below";
            // 
            // DInput
            // 
            DInput.Location = new Point(62, 186);
            DInput.Name = "DInput";
            DInput.Size = new Size(165, 41);
            DInput.TabIndex = 15;
            DInput.Unsigned = false;
            DInput.Value = 0;
            // 
            // TInput
            // 
            TInput.Location = new Point(62, 139);
            TInput.Name = "TInput";
            TInput.Size = new Size(165, 41);
            TInput.TabIndex = 14;
            TInput.Unsigned = false;
            TInput.Value = 0;
            // 
            // textBox2
            // 
            textBox2.BackColor = SystemColors.Control;
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new Font("Segoe UI", 16F);
            textBox2.Location = new Point(6, 352);
            textBox2.Multiline = true;
            textBox2.Name = "textBox2";
            textBox2.ReadOnly = true;
            textBox2.Size = new Size(385, 35);
            textBox2.TabIndex = 13;
            textBox2.TabStop = false;
            textBox2.Text = "5. Tap on Next";
            // 
            // CapturePose1Btn
            // 
            CapturePose1Btn.Font = new Font("Segoe UI", 18F);
            CapturePose1Btn.Image = Properties.Resources.location_48px;
            CapturePose1Btn.ImageAlign = ContentAlignment.MiddleLeft;
            CapturePose1Btn.Location = new Point(33, 283);
            CapturePose1Btn.Name = "CapturePose1Btn";
            CapturePose1Btn.Size = new Size(258, 60);
            CapturePose1Btn.TabIndex = 12;
            CapturePose1Btn.Text = "Capture Location";
            CapturePose1Btn.TextAlign = ContentAlignment.MiddleRight;
            CapturePose1Btn.UseVisualStyleBackColor = true;
            CapturePose1Btn.Click += CapturePose1Btn_Click;
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
            textBox1.Size = new Size(385, 159);
            textBox1.TabIndex = 1;
            textBox1.TabStop = false;
            textBox1.Text = "1. Drive tractor to a flat location\r\n2. Place a piece of wood to the right side in line with rear axle\r\n3. Measure the distances T and D";
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox1.Image = Properties.Resources.tractor_calib_up;
            pictureBox1.Location = new Point(359, 3);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(427, 294);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(textBox8);
            tabPage2.Controls.Add(CapturePose2Btn);
            tabPage2.Controls.Add(textBox9);
            tabPage2.Controls.Add(pictureBox2);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 420);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            // 
            // textBox8
            // 
            textBox8.BackColor = SystemColors.Control;
            textBox8.BorderStyle = BorderStyle.None;
            textBox8.Font = new Font("Segoe UI", 16F);
            textBox8.Location = new Point(6, 249);
            textBox8.Multiline = true;
            textBox8.Name = "textBox8";
            textBox8.ReadOnly = true;
            textBox8.Size = new Size(385, 35);
            textBox8.TabIndex = 20;
            textBox8.TabStop = false;
            textBox8.Text = "4. Tap on Next";
            // 
            // CapturePose2Btn
            // 
            CapturePose2Btn.Font = new Font("Segoe UI", 18F);
            CapturePose2Btn.Image = Properties.Resources.location_48px;
            CapturePose2Btn.ImageAlign = ContentAlignment.MiddleLeft;
            CapturePose2Btn.Location = new Point(33, 176);
            CapturePose2Btn.Name = "CapturePose2Btn";
            CapturePose2Btn.Size = new Size(258, 60);
            CapturePose2Btn.TabIndex = 19;
            CapturePose2Btn.Text = "Capture Location";
            CapturePose2Btn.TextAlign = ContentAlignment.MiddleRight;
            CapturePose2Btn.UseVisualStyleBackColor = true;
            CapturePose2Btn.Click += CapturePose2Btn_Click;
            // 
            // textBox9
            // 
            textBox9.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox9.BackColor = SystemColors.Control;
            textBox9.BorderStyle = BorderStyle.None;
            textBox9.Font = new Font("Segoe UI", 16F);
            textBox9.Location = new Point(6, 9);
            textBox9.Multiline = true;
            textBox9.Name = "textBox9";
            textBox9.ReadOnly = true;
            textBox9.Size = new Size(385, 159);
            textBox9.TabIndex = 18;
            textBox9.TabStop = false;
            textBox9.Text = "1. Drive tractor ahead, turn around and return to the pole\r\n2. Get D as close as possible to the value you measured (#mm)\r\n3. Tap on button below\r\n";
            // 
            // pictureBox2
            // 
            pictureBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureBox2.Image = Properties.Resources.tractor_calib_down;
            pictureBox2.Location = new Point(359, 6);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(427, 294);
            pictureBox2.TabIndex = 17;
            pictureBox2.TabStop = false;
            // 
            // tabPage3
            // 
            tabPage3.BackColor = SystemColors.Control;
            tabPage3.Controls.Add(ReturnBtn);
            tabPage3.Controls.Add(textBox10);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(792, 420);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "tabPage3";
            // 
            // ReturnBtn
            // 
            ReturnBtn.Font = new Font("Segoe UI", 18F);
            ReturnBtn.Image = Properties.Resources.calibration_48px;
            ReturnBtn.ImageAlign = ContentAlignment.MiddleLeft;
            ReturnBtn.Location = new Point(6, 47);
            ReturnBtn.Name = "ReturnBtn";
            ReturnBtn.Size = new Size(339, 60);
            ReturnBtn.TabIndex = 20;
            ReturnBtn.Text = "Return To Calibration List";
            ReturnBtn.TextAlign = ContentAlignment.MiddleRight;
            ReturnBtn.UseVisualStyleBackColor = true;
            ReturnBtn.Click += ReturnBtn_Click;
            // 
            // textBox10
            // 
            textBox10.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox10.BackColor = SystemColors.Control;
            textBox10.BorderStyle = BorderStyle.None;
            textBox10.Font = new Font("Segoe UI", 16F);
            textBox10.Location = new Point(6, 6);
            textBox10.Multiline = true;
            textBox10.Name = "textBox10";
            textBox10.ReadOnly = true;
            textBox10.Size = new Size(517, 35);
            textBox10.TabIndex = 19;
            textBox10.TabStop = false;
            textBox10.Text = "Result message\r\n";
            // 
            // CalibrateTractorAntennaWizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(Pages);
            Name = "CalibrateTractorAntennaWizard";
            Size = new Size(800, 448);
            Pages.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl Pages;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private PictureBox pictureBox1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Button CapturePose1Btn;
        private TextBox textBox4;
        private TextBox textBox3;
        private NumericInput DInput;
        private NumericInput TInput;
        private TextBox textBox7;
        private TextBox textBox6;
        private TextBox textBox5;
        private Button CapturePose2Btn;
        private TextBox textBox9;
        private PictureBox pictureBox2;
        private TextBox textBox8;
        private TabPage tabPage3;
        private TextBox textBox10;
        private Button ReturnBtn;
    }
}
