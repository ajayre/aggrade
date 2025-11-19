namespace AgGrade.Controls
{
    partial class AppSettingsEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppSettingsEditor));
            comPortSelector1 = new COMPortSelector();
            label1 = new Label();
            sectionTitle1 = new SectionTitle();
            PowerBtn = new Button();
            label2 = new Label();
            comPortSelector2 = new COMPortSelector();
            label3 = new Label();
            comPortSelector3 = new COMPortSelector();
            label4 = new Label();
            comPortSelector4 = new COMPortSelector();
            label5 = new Label();
            comPortSelector5 = new COMPortSelector();
            SuspendLayout();
            // 
            // comPortSelector1
            // 
            comPortSelector1.Location = new Point(173, 54);
            comPortSelector1.Name = "comPortSelector1";
            comPortSelector1.Size = new Size(151, 33);
            comPortSelector1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 14F);
            label1.Location = new Point(66, 58);
            label1.Name = "label1";
            label1.Size = new Size(101, 25);
            label1.TabIndex = 1;
            label1.Text = "Controller:";
            // 
            // sectionTitle1
            // 
            sectionTitle1.Dock = DockStyle.Top;
            sectionTitle1.Location = new Point(0, 0);
            sectionTitle1.Name = "sectionTitle1";
            sectionTitle1.Size = new Size(673, 48);
            sectionTitle1.TabIndex = 2;
            sectionTitle1.TitleText = "Application Settings";
            // 
            // PowerBtn
            // 
            PowerBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            PowerBtn.BackColor = SystemColors.Control;
            PowerBtn.Image = (Image)resources.GetObject("PowerBtn.Image");
            PowerBtn.Location = new Point(602, 9);
            PowerBtn.Name = "PowerBtn";
            PowerBtn.Size = new Size(60, 60);
            PowerBtn.TabIndex = 4;
            PowerBtn.UseVisualStyleBackColor = false;
            PowerBtn.Click += PowerBtn_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 14F);
            label2.Location = new Point(41, 97);
            label2.Name = "label2";
            label2.Size = new Size(126, 25);
            label2.TabIndex = 6;
            label2.Text = "Tractor GNSS:";
            // 
            // comPortSelector2
            // 
            comPortSelector2.Location = new Point(173, 93);
            comPortSelector2.Name = "comPortSelector2";
            comPortSelector2.Size = new Size(151, 33);
            comPortSelector2.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 14F);
            label3.Location = new Point(19, 136);
            label3.Name = "label3";
            label3.Size = new Size(148, 25);
            label3.TabIndex = 8;
            label3.Text = "Front Pan GNSS:";
            // 
            // comPortSelector3
            // 
            comPortSelector3.Location = new Point(173, 132);
            comPortSelector3.Name = "comPortSelector3";
            comPortSelector3.Size = new Size(151, 33);
            comPortSelector3.TabIndex = 7;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 14F);
            label4.Location = new Point(26, 175);
            label4.Name = "label4";
            label4.Size = new Size(141, 25);
            label4.TabIndex = 10;
            label4.Text = "Rear Pan GNSS:";
            // 
            // comPortSelector4
            // 
            comPortSelector4.Location = new Point(173, 171);
            comPortSelector4.Name = "comPortSelector4";
            comPortSelector4.Size = new Size(151, 33);
            comPortSelector4.TabIndex = 9;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI", 14F);
            label5.Location = new Point(52, 214);
            label5.Name = "label5";
            label5.Size = new Size(115, 25);
            label5.TabIndex = 12;
            label5.Text = "Slave Tablet:";
            // 
            // comPortSelector5
            // 
            comPortSelector5.Location = new Point(173, 210);
            comPortSelector5.Name = "comPortSelector5";
            comPortSelector5.Size = new Size(151, 33);
            comPortSelector5.TabIndex = 11;
            // 
            // AppSettingsEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            Controls.Add(label5);
            Controls.Add(comPortSelector5);
            Controls.Add(label4);
            Controls.Add(comPortSelector4);
            Controls.Add(label3);
            Controls.Add(comPortSelector3);
            Controls.Add(label2);
            Controls.Add(comPortSelector2);
            Controls.Add(PowerBtn);
            Controls.Add(sectionTitle1);
            Controls.Add(label1);
            Controls.Add(comPortSelector1);
            Name = "AppSettingsEditor";
            Size = new Size(673, 313);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private COMPortSelector comPortSelector1;
        private Label label1;
        private SectionTitle sectionTitle1;
        private Button PowerBtn;
        private Label label2;
        private COMPortSelector comPortSelector2;
        private Label label3;
        private COMPortSelector comPortSelector3;
        private Label label4;
        private COMPortSelector comPortSelector4;
        private Label label5;
        private COMPortSelector comPortSelector5;
    }
}
