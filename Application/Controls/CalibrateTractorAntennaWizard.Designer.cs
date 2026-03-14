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
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            textBox7 = new TextBox();
            textBox6 = new TextBox();
            textBox5 = new TextBox();
            textBox4 = new TextBox();
            textBox3 = new TextBox();
            numericInput2 = new NumericInput();
            numericInput1 = new NumericInput();
            textBox2 = new TextBox();
            BackBtn = new Button();
            textBox1 = new TextBox();
            pictureBox1 = new PictureBox();
            tabPage2 = new TabPage();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 448);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(textBox7);
            tabPage1.Controls.Add(textBox6);
            tabPage1.Controls.Add(textBox5);
            tabPage1.Controls.Add(textBox4);
            tabPage1.Controls.Add(textBox3);
            tabPage1.Controls.Add(numericInput2);
            tabPage1.Controls.Add(numericInput1);
            tabPage1.Controls.Add(textBox2);
            tabPage1.Controls.Add(BackBtn);
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
            textBox3.Text = "4. Tap on Button Below";
            // 
            // numericInput2
            // 
            numericInput2.Location = new Point(62, 186);
            numericInput2.Name = "numericInput2";
            numericInput2.Size = new Size(165, 41);
            numericInput2.TabIndex = 15;
            numericInput2.Unsigned = false;
            numericInput2.Value = 0;
            // 
            // numericInput1
            // 
            numericInput1.Location = new Point(62, 139);
            numericInput1.Name = "numericInput1";
            numericInput1.Size = new Size(165, 41);
            numericInput1.TabIndex = 14;
            numericInput1.Unsigned = false;
            numericInput1.Value = 0;
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
            textBox2.Text = "5. Tap on Next";
            // 
            // BackBtn
            // 
            BackBtn.Font = new Font("Segoe UI", 18F);
            BackBtn.Image = Properties.Resources.location_48px;
            BackBtn.ImageAlign = ContentAlignment.MiddleLeft;
            BackBtn.Location = new Point(33, 283);
            BackBtn.Name = "BackBtn";
            BackBtn.Size = new Size(258, 60);
            BackBtn.TabIndex = 12;
            BackBtn.Text = "Capture Location";
            BackBtn.TextAlign = ContentAlignment.MiddleRight;
            BackBtn.UseVisualStyleBackColor = true;
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
            tabPage2.BackColor = Color.DarkSeaGreen;
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 420);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "tabPage2";
            // 
            // CalibrateTractorAntennaWizard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl1);
            Name = "CalibrateTractorAntennaWizard";
            Size = new Size(800, 448);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private PictureBox pictureBox1;
        private TextBox textBox1;
        private TextBox textBox2;
        private Button BackBtn;
        private TextBox textBox4;
        private TextBox textBox3;
        private NumericInput numericInput2;
        private NumericInput numericInput1;
        private TextBox textBox7;
        private TextBox textBox6;
        private TextBox textBox5;
    }
}
