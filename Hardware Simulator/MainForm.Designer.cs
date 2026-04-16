namespace HardwareSim
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            EStopBtn = new Button();
            ClearEStopBtn = new Button();
            TractorIMUFoundBtn = new Button();
            TractorIMULostBtn = new Button();
            FrontIMULostBtn = new Button();
            FrontIMUFoundBtn = new Button();
            RearIMULostBtn = new Button();
            RearIMUFoundBtn = new Button();
            tabControl1 = new TabControl();
            MiscPage = new TabPage();
            RearBucketLostBtn = new Button();
            RearBucketFoundBtn = new Button();
            FrontBucketLostBtn = new Button();
            FrontBucketFoundBtn = new Button();
            FrontApronLostBtn = new Button();
            FrontApronFoundBtn = new Button();
            SecondaryTabletBtn = new Button();
            RearHeightLostBtn = new Button();
            FrontHeightLostBtn = new Button();
            RearHeightFoundBtn = new Button();
            FrontHeightFoundBtn = new Button();
            GNSSPage = new TabPage();
            label4 = new Label();
            RearJoystickDownBtn = new Button();
            RearJoystickUpBtn = new Button();
            label3 = new Label();
            FrontJoystickDownBtn = new Button();
            FrontJoystickUpBtn = new Button();
            RearToggleDumpingBtn = new Button();
            FrontToggleDumpingBtn = new Button();
            RearToggleCuttingBtn = new Button();
            FrontToggleCuttingBtn = new Button();
            AutoDriveBtn = new Button();
            SteerLeftBtn = new Button();
            SteerRightBtn = new Button();
            ReverseBtn = new Button();
            ForwardsBtn = new Button();
            SetLocationBtn = new Button();
            label2 = new Label();
            label1 = new Label();
            LongitudeInput = new TextBox();
            LatitudeInput = new TextBox();
            tabControl1.SuspendLayout();
            MiscPage.SuspendLayout();
            GNSSPage.SuspendLayout();
            SuspendLayout();
            // 
            // EStopBtn
            // 
            EStopBtn.Location = new Point(6, 6);
            EStopBtn.Name = "EStopBtn";
            EStopBtn.Size = new Size(75, 23);
            EStopBtn.TabIndex = 0;
            EStopBtn.Text = "Set ESTOP";
            EStopBtn.UseVisualStyleBackColor = true;
            EStopBtn.Click += EStopBtn_Click;
            // 
            // ClearEStopBtn
            // 
            ClearEStopBtn.Location = new Point(87, 6);
            ClearEStopBtn.Name = "ClearEStopBtn";
            ClearEStopBtn.Size = new Size(92, 23);
            ClearEStopBtn.TabIndex = 1;
            ClearEStopBtn.Text = "Clear ESTOP";
            ClearEStopBtn.UseVisualStyleBackColor = true;
            ClearEStopBtn.Click += ClearEStopBtn_Click;
            // 
            // TractorIMUFoundBtn
            // 
            TractorIMUFoundBtn.Location = new Point(6, 35);
            TractorIMUFoundBtn.Name = "TractorIMUFoundBtn";
            TractorIMUFoundBtn.Size = new Size(127, 23);
            TractorIMUFoundBtn.TabIndex = 2;
            TractorIMUFoundBtn.Text = "Tractor IMU Found";
            TractorIMUFoundBtn.UseVisualStyleBackColor = true;
            TractorIMUFoundBtn.Click += TractorIMUFoundBtn_Click;
            // 
            // TractorIMULostBtn
            // 
            TractorIMULostBtn.Location = new Point(139, 35);
            TractorIMULostBtn.Name = "TractorIMULostBtn";
            TractorIMULostBtn.Size = new Size(127, 23);
            TractorIMULostBtn.TabIndex = 3;
            TractorIMULostBtn.Text = "Tractor IMU Lost";
            TractorIMULostBtn.UseVisualStyleBackColor = true;
            TractorIMULostBtn.Click += TractorIMULostBtn_Click;
            // 
            // FrontIMULostBtn
            // 
            FrontIMULostBtn.Location = new Point(139, 64);
            FrontIMULostBtn.Name = "FrontIMULostBtn";
            FrontIMULostBtn.Size = new Size(127, 23);
            FrontIMULostBtn.TabIndex = 5;
            FrontIMULostBtn.Text = "Front IMU Lost";
            FrontIMULostBtn.UseVisualStyleBackColor = true;
            FrontIMULostBtn.Click += FrontIMULostBtn_Click;
            // 
            // FrontIMUFoundBtn
            // 
            FrontIMUFoundBtn.Location = new Point(6, 64);
            FrontIMUFoundBtn.Name = "FrontIMUFoundBtn";
            FrontIMUFoundBtn.Size = new Size(127, 23);
            FrontIMUFoundBtn.TabIndex = 4;
            FrontIMUFoundBtn.Text = "Front IMU Found";
            FrontIMUFoundBtn.UseVisualStyleBackColor = true;
            FrontIMUFoundBtn.Click += FrontIMUFoundBtn_Click;
            // 
            // RearIMULostBtn
            // 
            RearIMULostBtn.Location = new Point(139, 93);
            RearIMULostBtn.Name = "RearIMULostBtn";
            RearIMULostBtn.Size = new Size(127, 23);
            RearIMULostBtn.TabIndex = 7;
            RearIMULostBtn.Text = "Rear IMU Lost";
            RearIMULostBtn.UseVisualStyleBackColor = true;
            RearIMULostBtn.Click += RearIMULostBtn_Click;
            // 
            // RearIMUFoundBtn
            // 
            RearIMUFoundBtn.Location = new Point(6, 93);
            RearIMUFoundBtn.Name = "RearIMUFoundBtn";
            RearIMUFoundBtn.Size = new Size(127, 23);
            RearIMUFoundBtn.TabIndex = 6;
            RearIMUFoundBtn.Text = "Rear IMU Found";
            RearIMUFoundBtn.UseVisualStyleBackColor = true;
            RearIMUFoundBtn.Click += RearIMUFoundBtn_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(MiscPage);
            tabControl1.Controls.Add(GNSSPage);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(605, 450);
            tabControl1.TabIndex = 8;
            // 
            // MiscPage
            // 
            MiscPage.Controls.Add(RearBucketLostBtn);
            MiscPage.Controls.Add(RearBucketFoundBtn);
            MiscPage.Controls.Add(FrontBucketLostBtn);
            MiscPage.Controls.Add(FrontBucketFoundBtn);
            MiscPage.Controls.Add(FrontApronLostBtn);
            MiscPage.Controls.Add(FrontApronFoundBtn);
            MiscPage.Controls.Add(SecondaryTabletBtn);
            MiscPage.Controls.Add(RearHeightLostBtn);
            MiscPage.Controls.Add(FrontHeightLostBtn);
            MiscPage.Controls.Add(RearHeightFoundBtn);
            MiscPage.Controls.Add(FrontHeightFoundBtn);
            MiscPage.Controls.Add(EStopBtn);
            MiscPage.Controls.Add(RearIMULostBtn);
            MiscPage.Controls.Add(ClearEStopBtn);
            MiscPage.Controls.Add(RearIMUFoundBtn);
            MiscPage.Controls.Add(TractorIMUFoundBtn);
            MiscPage.Controls.Add(FrontIMULostBtn);
            MiscPage.Controls.Add(TractorIMULostBtn);
            MiscPage.Controls.Add(FrontIMUFoundBtn);
            MiscPage.Location = new Point(4, 24);
            MiscPage.Name = "MiscPage";
            MiscPage.Padding = new Padding(3);
            MiscPage.Size = new Size(597, 422);
            MiscPage.TabIndex = 0;
            MiscPage.Text = "Misc";
            MiscPage.UseVisualStyleBackColor = true;
            // 
            // RearBucketLostBtn
            // 
            RearBucketLostBtn.Location = new Point(173, 238);
            RearBucketLostBtn.Name = "RearBucketLostBtn";
            RearBucketLostBtn.Size = new Size(159, 23);
            RearBucketLostBtn.TabIndex = 18;
            RearBucketLostBtn.Text = "Rear Bucket IMU Lost";
            RearBucketLostBtn.UseVisualStyleBackColor = true;
            RearBucketLostBtn.Click += RearBucketLostBtn_Click;
            // 
            // RearBucketFoundBtn
            // 
            RearBucketFoundBtn.Location = new Point(8, 238);
            RearBucketFoundBtn.Name = "RearBucketFoundBtn";
            RearBucketFoundBtn.Size = new Size(159, 23);
            RearBucketFoundBtn.TabIndex = 17;
            RearBucketFoundBtn.Text = "Rear Bucket IMU Found";
            RearBucketFoundBtn.UseVisualStyleBackColor = true;
            RearBucketFoundBtn.Click += RearBucketFoundBtn_Click;
            // 
            // FrontBucketLostBtn
            // 
            FrontBucketLostBtn.Location = new Point(173, 209);
            FrontBucketLostBtn.Name = "FrontBucketLostBtn";
            FrontBucketLostBtn.Size = new Size(159, 23);
            FrontBucketLostBtn.TabIndex = 16;
            FrontBucketLostBtn.Text = "Front Bucket IMU Lost";
            FrontBucketLostBtn.UseVisualStyleBackColor = true;
            FrontBucketLostBtn.Click += FrontBucketLostBtn_Click;
            // 
            // FrontBucketFoundBtn
            // 
            FrontBucketFoundBtn.Location = new Point(8, 209);
            FrontBucketFoundBtn.Name = "FrontBucketFoundBtn";
            FrontBucketFoundBtn.Size = new Size(159, 23);
            FrontBucketFoundBtn.TabIndex = 15;
            FrontBucketFoundBtn.Text = "Front Bucket IMU Found";
            FrontBucketFoundBtn.UseVisualStyleBackColor = true;
            FrontBucketFoundBtn.Click += FrontBucketFoundBtn_Click;
            // 
            // FrontApronLostBtn
            // 
            FrontApronLostBtn.Location = new Point(173, 180);
            FrontApronLostBtn.Name = "FrontApronLostBtn";
            FrontApronLostBtn.Size = new Size(159, 23);
            FrontApronLostBtn.TabIndex = 14;
            FrontApronLostBtn.Text = "Front Apron IMU Lost";
            FrontApronLostBtn.UseVisualStyleBackColor = true;
            FrontApronLostBtn.Click += FrontApronLostBtn_Click;
            // 
            // FrontApronFoundBtn
            // 
            FrontApronFoundBtn.Location = new Point(8, 180);
            FrontApronFoundBtn.Name = "FrontApronFoundBtn";
            FrontApronFoundBtn.Size = new Size(159, 23);
            FrontApronFoundBtn.TabIndex = 13;
            FrontApronFoundBtn.Text = "Front Apron IMU Found";
            FrontApronFoundBtn.UseVisualStyleBackColor = true;
            FrontApronFoundBtn.Click += FrontApronFoundBtn_Click;
            // 
            // SecondaryTabletBtn
            // 
            SecondaryTabletBtn.Location = new Point(292, 6);
            SecondaryTabletBtn.Name = "SecondaryTabletBtn";
            SecondaryTabletBtn.Size = new Size(127, 23);
            SecondaryTabletBtn.TabIndex = 12;
            SecondaryTabletBtn.Text = "Secondary Tablet";
            SecondaryTabletBtn.UseVisualStyleBackColor = true;
            SecondaryTabletBtn.Click += SecondaryTabletBtn_Click;
            // 
            // RearHeightLostBtn
            // 
            RearHeightLostBtn.Location = new Point(139, 151);
            RearHeightLostBtn.Name = "RearHeightLostBtn";
            RearHeightLostBtn.Size = new Size(127, 23);
            RearHeightLostBtn.TabIndex = 11;
            RearHeightLostBtn.Text = "Rear Height Lost";
            RearHeightLostBtn.UseVisualStyleBackColor = true;
            RearHeightLostBtn.Click += RearHeightLostBtn_Click;
            // 
            // FrontHeightLostBtn
            // 
            FrontHeightLostBtn.Location = new Point(139, 122);
            FrontHeightLostBtn.Name = "FrontHeightLostBtn";
            FrontHeightLostBtn.Size = new Size(127, 23);
            FrontHeightLostBtn.TabIndex = 10;
            FrontHeightLostBtn.Text = "Front Height Lost";
            FrontHeightLostBtn.UseVisualStyleBackColor = true;
            FrontHeightLostBtn.Click += FrontHeightLostBtn_Click;
            // 
            // RearHeightFoundBtn
            // 
            RearHeightFoundBtn.Location = new Point(6, 151);
            RearHeightFoundBtn.Name = "RearHeightFoundBtn";
            RearHeightFoundBtn.Size = new Size(127, 23);
            RearHeightFoundBtn.TabIndex = 9;
            RearHeightFoundBtn.Text = "Rear Height Found";
            RearHeightFoundBtn.UseVisualStyleBackColor = true;
            RearHeightFoundBtn.Click += RearHeightFoundBtn_Click;
            // 
            // FrontHeightFoundBtn
            // 
            FrontHeightFoundBtn.Location = new Point(6, 122);
            FrontHeightFoundBtn.Name = "FrontHeightFoundBtn";
            FrontHeightFoundBtn.Size = new Size(127, 23);
            FrontHeightFoundBtn.TabIndex = 8;
            FrontHeightFoundBtn.Text = "Front Height Found";
            FrontHeightFoundBtn.UseVisualStyleBackColor = true;
            FrontHeightFoundBtn.Click += FrontHeightFoundBtn_Click;
            // 
            // GNSSPage
            // 
            GNSSPage.Controls.Add(label4);
            GNSSPage.Controls.Add(RearJoystickDownBtn);
            GNSSPage.Controls.Add(RearJoystickUpBtn);
            GNSSPage.Controls.Add(label3);
            GNSSPage.Controls.Add(FrontJoystickDownBtn);
            GNSSPage.Controls.Add(FrontJoystickUpBtn);
            GNSSPage.Controls.Add(RearToggleDumpingBtn);
            GNSSPage.Controls.Add(FrontToggleDumpingBtn);
            GNSSPage.Controls.Add(RearToggleCuttingBtn);
            GNSSPage.Controls.Add(FrontToggleCuttingBtn);
            GNSSPage.Controls.Add(AutoDriveBtn);
            GNSSPage.Controls.Add(SteerLeftBtn);
            GNSSPage.Controls.Add(SteerRightBtn);
            GNSSPage.Controls.Add(ReverseBtn);
            GNSSPage.Controls.Add(ForwardsBtn);
            GNSSPage.Controls.Add(SetLocationBtn);
            GNSSPage.Controls.Add(label2);
            GNSSPage.Controls.Add(label1);
            GNSSPage.Controls.Add(LongitudeInput);
            GNSSPage.Controls.Add(LatitudeInput);
            GNSSPage.Location = new Point(4, 24);
            GNSSPage.Name = "GNSSPage";
            GNSSPage.Padding = new Padding(3);
            GNSSPage.Size = new Size(597, 422);
            GNSSPage.TabIndex = 1;
            GNSSPage.Text = "GNSS";
            GNSSPage.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(121, 242);
            label4.Name = "label4";
            label4.Size = new Size(77, 15);
            label4.TabIndex = 18;
            label4.Text = "Rear Joystick:";
            // 
            // RearJoystickDownBtn
            // 
            RearJoystickDownBtn.Image = Properties.Resources.down_48px;
            RearJoystickDownBtn.Location = new Point(132, 322);
            RearJoystickDownBtn.Name = "RearJoystickDownBtn";
            RearJoystickDownBtn.Size = new Size(56, 56);
            RearJoystickDownBtn.TabIndex = 17;
            RearJoystickDownBtn.UseVisualStyleBackColor = true;
            RearJoystickDownBtn.MouseDown += RearJoystickDownBtn_MouseDown;
            RearJoystickDownBtn.MouseUp += RearJoystickDownBtn_MouseUp;
            // 
            // RearJoystickUpBtn
            // 
            RearJoystickUpBtn.Image = Properties.Resources.up_48px;
            RearJoystickUpBtn.Location = new Point(132, 260);
            RearJoystickUpBtn.Name = "RearJoystickUpBtn";
            RearJoystickUpBtn.Size = new Size(56, 56);
            RearJoystickUpBtn.TabIndex = 16;
            RearJoystickUpBtn.UseVisualStyleBackColor = true;
            RearJoystickUpBtn.MouseDown += RearJoystickUpBtn_MouseDown;
            RearJoystickUpBtn.MouseUp += RearJoystickUpBtn_MouseUp;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(8, 242);
            label3.Name = "label3";
            label3.Size = new Size(82, 15);
            label3.TabIndex = 15;
            label3.Text = "Front Joystick:";
            // 
            // FrontJoystickDownBtn
            // 
            FrontJoystickDownBtn.Image = Properties.Resources.down_48px;
            FrontJoystickDownBtn.Location = new Point(19, 322);
            FrontJoystickDownBtn.Name = "FrontJoystickDownBtn";
            FrontJoystickDownBtn.Size = new Size(56, 56);
            FrontJoystickDownBtn.TabIndex = 14;
            FrontJoystickDownBtn.UseVisualStyleBackColor = true;
            FrontJoystickDownBtn.MouseDown += FrontJoystickDownBtn_MouseDown;
            FrontJoystickDownBtn.MouseUp += FrontJoystickDownBtn_MouseUp;
            // 
            // FrontJoystickUpBtn
            // 
            FrontJoystickUpBtn.Image = Properties.Resources.up_48px;
            FrontJoystickUpBtn.Location = new Point(19, 260);
            FrontJoystickUpBtn.Name = "FrontJoystickUpBtn";
            FrontJoystickUpBtn.Size = new Size(56, 56);
            FrontJoystickUpBtn.TabIndex = 13;
            FrontJoystickUpBtn.UseVisualStyleBackColor = true;
            FrontJoystickUpBtn.MouseDown += FrontJoystickUpBtn_MouseDown;
            FrontJoystickUpBtn.MouseUp += FrontJoystickUpBtn_MouseUp;
            // 
            // RearToggleDumpingBtn
            // 
            RearToggleDumpingBtn.Location = new Point(193, 202);
            RearToggleDumpingBtn.Name = "RearToggleDumpingBtn";
            RearToggleDumpingBtn.Size = new Size(148, 23);
            RearToggleDumpingBtn.TabIndex = 12;
            RearToggleDumpingBtn.Text = "Rear Toggle Dumping";
            RearToggleDumpingBtn.UseVisualStyleBackColor = true;
            RearToggleDumpingBtn.Click += RearToggleDumpingBtn_Click;
            // 
            // FrontToggleDumpingBtn
            // 
            FrontToggleDumpingBtn.Location = new Point(193, 173);
            FrontToggleDumpingBtn.Name = "FrontToggleDumpingBtn";
            FrontToggleDumpingBtn.Size = new Size(148, 23);
            FrontToggleDumpingBtn.TabIndex = 11;
            FrontToggleDumpingBtn.Text = "Front Toggle Dumping";
            FrontToggleDumpingBtn.UseVisualStyleBackColor = true;
            FrontToggleDumpingBtn.Click += FrontToggleDumpingBtn_Click;
            // 
            // RearToggleCuttingBtn
            // 
            RearToggleCuttingBtn.Location = new Point(8, 202);
            RearToggleCuttingBtn.Name = "RearToggleCuttingBtn";
            RearToggleCuttingBtn.Size = new Size(148, 23);
            RearToggleCuttingBtn.TabIndex = 10;
            RearToggleCuttingBtn.Text = "Rear Toggle Cutting";
            RearToggleCuttingBtn.UseVisualStyleBackColor = true;
            RearToggleCuttingBtn.Click += RearToggleCuttingBtn_Click;
            // 
            // FrontToggleCuttingBtn
            // 
            FrontToggleCuttingBtn.Location = new Point(8, 173);
            FrontToggleCuttingBtn.Name = "FrontToggleCuttingBtn";
            FrontToggleCuttingBtn.Size = new Size(148, 23);
            FrontToggleCuttingBtn.TabIndex = 9;
            FrontToggleCuttingBtn.Text = "Front Toggle Cutting";
            FrontToggleCuttingBtn.UseVisualStyleBackColor = true;
            FrontToggleCuttingBtn.Click += FrontToggleCuttingBtn_Click;
            // 
            // AutoDriveBtn
            // 
            AutoDriveBtn.Location = new Point(220, 70);
            AutoDriveBtn.Name = "AutoDriveBtn";
            AutoDriveBtn.Size = new Size(121, 23);
            AutoDriveBtn.TabIndex = 19;
            AutoDriveBtn.Text = "Auto Drive";
            AutoDriveBtn.UseVisualStyleBackColor = true;
            AutoDriveBtn.Click += AutoDriveBtn_Click;
            // 
            // SteerLeftBtn
            // 
            SteerLeftBtn.Image = Properties.Resources.left_48px;
            SteerLeftBtn.Location = new Point(8, 70);
            SteerLeftBtn.Name = "SteerLeftBtn";
            SteerLeftBtn.Size = new Size(56, 56);
            SteerLeftBtn.TabIndex = 8;
            SteerLeftBtn.UseVisualStyleBackColor = true;
            SteerLeftBtn.Click += SteerLeftBtn_Click;
            // 
            // SteerRightBtn
            // 
            SteerRightBtn.Image = Properties.Resources.right_48px;
            SteerRightBtn.Location = new Point(132, 70);
            SteerRightBtn.Name = "SteerRightBtn";
            SteerRightBtn.Size = new Size(56, 56);
            SteerRightBtn.TabIndex = 7;
            SteerRightBtn.UseVisualStyleBackColor = true;
            SteerRightBtn.Click += SteerRightBtn_Click;
            // 
            // ReverseBtn
            // 
            ReverseBtn.Image = Properties.Resources.down_48px;
            ReverseBtn.Location = new Point(70, 98);
            ReverseBtn.Name = "ReverseBtn";
            ReverseBtn.Size = new Size(56, 56);
            ReverseBtn.TabIndex = 6;
            ReverseBtn.UseVisualStyleBackColor = true;
            ReverseBtn.Click += ReverseBtn_Click;
            // 
            // ForwardsBtn
            // 
            ForwardsBtn.Image = Properties.Resources.up_48px;
            ForwardsBtn.Location = new Point(70, 36);
            ForwardsBtn.Name = "ForwardsBtn";
            ForwardsBtn.Size = new Size(56, 56);
            ForwardsBtn.TabIndex = 5;
            ForwardsBtn.UseVisualStyleBackColor = true;
            ForwardsBtn.Click += ForwardsBtn_Click;
            // 
            // SetLocationBtn
            // 
            SetLocationBtn.Location = new Point(423, 6);
            SetLocationBtn.Name = "SetLocationBtn";
            SetLocationBtn.Size = new Size(55, 23);
            SetLocationBtn.TabIndex = 4;
            SetLocationBtn.Text = "Set";
            SetLocationBtn.UseVisualStyleBackColor = true;
            SetLocationBtn.Click += SetLocationBtn_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(220, 9);
            label2.Name = "label2";
            label2.Size = new Size(64, 15);
            label2.TabIndex = 3;
            label2.Text = "Longitude:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(22, 10);
            label1.Name = "label1";
            label1.Size = new Size(53, 15);
            label1.TabIndex = 2;
            label1.Text = "Latitude:";
            // 
            // LongitudeInput
            // 
            LongitudeInput.Location = new Point(290, 6);
            LongitudeInput.Name = "LongitudeInput";
            LongitudeInput.Size = new Size(127, 23);
            LongitudeInput.TabIndex = 1;
            // 
            // LatitudeInput
            // 
            LatitudeInput.Location = new Point(81, 7);
            LatitudeInput.Name = "LatitudeInput";
            LatitudeInput.Size = new Size(127, 23);
            LatitudeInput.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(605, 450);
            Controls.Add(tabControl1);
            Name = "MainForm";
            Text = "Hardware Simulator";
            tabControl1.ResumeLayout(false);
            MiscPage.ResumeLayout(false);
            GNSSPage.ResumeLayout(false);
            GNSSPage.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button EStopBtn;
        private Button ClearEStopBtn;
        private Button TractorIMUFoundBtn;
        private Button TractorIMULostBtn;
        private Button FrontIMULostBtn;
        private Button FrontIMUFoundBtn;
        private Button RearIMULostBtn;
        private Button RearIMUFoundBtn;
        private TabControl tabControl1;
        private TabPage MiscPage;
        private TabPage GNSSPage;
        private Button RearHeightLostBtn;
        private Button FrontHeightLostBtn;
        private Button RearHeightFoundBtn;
        private Button FrontHeightFoundBtn;
        private Label label2;
        private Label label1;
        private TextBox LongitudeInput;
        private TextBox LatitudeInput;
        private Button SetLocationBtn;
        private Button ForwardsBtn;
        private Button ReverseBtn;
        private Button SteerLeftBtn;
        private Button SteerRightBtn;
        private Button FrontToggleCuttingBtn;
        private Button RearToggleCuttingBtn;
        private Button RearToggleDumpingBtn;
        private Button FrontToggleDumpingBtn;
        private Label label3;
        private Button FrontJoystickDownBtn;
        private Button FrontJoystickUpBtn;
        private Label label4;
        private Button RearJoystickDownBtn;
        private Button RearJoystickUpBtn;
        private Button AutoDriveBtn;
        private Button SecondaryTabletBtn;
        private Button FrontApronFoundBtn;
        private Button RearBucketLostBtn;
        private Button RearBucketFoundBtn;
        private Button FrontBucketLostBtn;
        private Button FrontBucketFoundBtn;
        private Button FrontApronLostBtn;
    }
}
