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
            RearHeightLostBtn = new Button();
            FrontHeightLostBtn = new Button();
            RearHeightFoundBtn = new Button();
            FrontHeightFoundBtn = new Button();
            GNSSPage = new TabPage();
            RearToggleCuttingBtn = new Button();
            FrontToggleCuttingBtn = new Button();
            SteerLeftBtn = new Button();
            SteerRightBtn = new Button();
            ReverseBtn = new Button();
            ForwardsBtn = new Button();
            SetLocationBtn = new Button();
            label2 = new Label();
            label1 = new Label();
            LongitudeInput = new TextBox();
            LatitudeInput = new TextBox();
            FrontToggleDumpingBtn = new Button();
            RearToggleDumpingBtn = new Button();
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
            GNSSPage.Controls.Add(RearToggleDumpingBtn);
            GNSSPage.Controls.Add(FrontToggleDumpingBtn);
            GNSSPage.Controls.Add(RearToggleCuttingBtn);
            GNSSPage.Controls.Add(FrontToggleCuttingBtn);
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
    }
}
