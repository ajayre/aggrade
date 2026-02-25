namespace AgGrade
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panel3 = new Panel();
            RearPanIndicator = new Panel();
            FrontPanIndicator = new Panel();
            ZoomFitBtn = new Button();
            ZoomInBtn = new Button();
            ZoomOutBtn = new Button();
            RearBladeControlBtn = new AgGrade.Controls.IndicatorButton();
            FrontBladeControlBtn = new AgGrade.Controls.IndicatorButton();
            panel1 = new Panel();
            StatusBtn = new Button();
            MapBtn = new Button();
            SurveyBtn = new Button();
            CalibrationBtn = new Button();
            EditSettingsBtn = new Button();
            EditEquipmentBtn = new Button();
            ContentPanel = new Panel();
            StatusPanel = new Panel();
            StatusBar = new AgGrade.Controls.StatusBar();
            OpenFieldBtn = new Button();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            StatusPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panel3
            // 
            panel3.Controls.Add(RearPanIndicator);
            panel3.Controls.Add(FrontPanIndicator);
            panel3.Controls.Add(ZoomFitBtn);
            panel3.Controls.Add(ZoomInBtn);
            panel3.Controls.Add(ZoomOutBtn);
            panel3.Controls.Add(RearBladeControlBtn);
            panel3.Controls.Add(FrontBladeControlBtn);
            panel3.Dock = DockStyle.Left;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(76, 740);
            panel3.TabIndex = 2;
            // 
            // RearPanIndicator
            // 
            RearPanIndicator.BackColor = Color.DarkGoldenrod;
            RearPanIndicator.BackgroundImage = (Image)resources.GetObject("RearPanIndicator.BackgroundImage");
            RearPanIndicator.BackgroundImageLayout = ImageLayout.Center;
            RearPanIndicator.Location = new Point(8, 273);
            RearPanIndicator.Name = "RearPanIndicator";
            RearPanIndicator.Size = new Size(60, 60);
            RearPanIndicator.TabIndex = 10;
            // 
            // FrontPanIndicator
            // 
            FrontPanIndicator.BackColor = Color.RoyalBlue;
            FrontPanIndicator.BackgroundImage = (Image)resources.GetObject("FrontPanIndicator.BackgroundImage");
            FrontPanIndicator.BackgroundImageLayout = ImageLayout.Center;
            FrontPanIndicator.Location = new Point(8, 207);
            FrontPanIndicator.Name = "FrontPanIndicator";
            FrontPanIndicator.Size = new Size(60, 60);
            FrontPanIndicator.TabIndex = 9;
            // 
            // ZoomFitBtn
            // 
            ZoomFitBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ZoomFitBtn.Image = (Image)resources.GetObject("ZoomFitBtn.Image");
            ZoomFitBtn.Location = new Point(8, 542);
            ZoomFitBtn.Name = "ZoomFitBtn";
            ZoomFitBtn.Size = new Size(60, 60);
            ZoomFitBtn.TabIndex = 8;
            ZoomFitBtn.UseVisualStyleBackColor = true;
            ZoomFitBtn.Click += ZoomFitBtn_Click;
            // 
            // ZoomInBtn
            // 
            ZoomInBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ZoomInBtn.Image = (Image)resources.GetObject("ZoomInBtn.Image");
            ZoomInBtn.Location = new Point(8, 608);
            ZoomInBtn.Name = "ZoomInBtn";
            ZoomInBtn.Size = new Size(60, 60);
            ZoomInBtn.TabIndex = 7;
            ZoomInBtn.UseVisualStyleBackColor = true;
            ZoomInBtn.Click += ZoomInBtn_Click;
            // 
            // ZoomOutBtn
            // 
            ZoomOutBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ZoomOutBtn.Image = (Image)resources.GetObject("ZoomOutBtn.Image");
            ZoomOutBtn.Location = new Point(8, 674);
            ZoomOutBtn.Name = "ZoomOutBtn";
            ZoomOutBtn.Size = new Size(60, 60);
            ZoomOutBtn.TabIndex = 6;
            ZoomOutBtn.UseVisualStyleBackColor = true;
            ZoomOutBtn.Click += ZoomOutBtn_Click;
            // 
            // RearBladeControlBtn
            // 
            RearBladeControlBtn.Image = (Image)resources.GetObject("RearBladeControlBtn.Image");
            RearBladeControlBtn.Indicator = AgGrade.Controls.IndicatorButton.IndicatorColor.Red;
            RearBladeControlBtn.Location = new Point(8, 90);
            RearBladeControlBtn.Name = "RearBladeControlBtn";
            RearBladeControlBtn.Size = new Size(60, 78);
            RearBladeControlBtn.TabIndex = 1;
            RearBladeControlBtn.OnButtonClicked += RearBladeControlBtn_OnButtonClicked;
            // 
            // FrontBladeControlBtn
            // 
            FrontBladeControlBtn.Image = (Image)resources.GetObject("FrontBladeControlBtn.Image");
            FrontBladeControlBtn.Indicator = AgGrade.Controls.IndicatorButton.IndicatorColor.Red;
            FrontBladeControlBtn.Location = new Point(8, 6);
            FrontBladeControlBtn.Name = "FrontBladeControlBtn";
            FrontBladeControlBtn.Size = new Size(60, 78);
            FrontBladeControlBtn.TabIndex = 0;
            FrontBladeControlBtn.OnButtonClicked += FrontBladeControlBtn_OnButtonClicked;
            // 
            // panel1
            // 
            panel1.Controls.Add(OpenFieldBtn);
            panel1.Controls.Add(StatusBtn);
            panel1.Controls.Add(MapBtn);
            panel1.Controls.Add(SurveyBtn);
            panel1.Controls.Add(CalibrationBtn);
            panel1.Controls.Add(EditSettingsBtn);
            panel1.Controls.Add(EditEquipmentBtn);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(969, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(76, 740);
            panel1.TabIndex = 3;
            // 
            // StatusBtn
            // 
            StatusBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            StatusBtn.Image = (Image)resources.GetObject("StatusBtn.Image");
            StatusBtn.Location = new Point(8, 138);
            StatusBtn.Name = "StatusBtn";
            StatusBtn.Size = new Size(60, 60);
            StatusBtn.TabIndex = 6;
            StatusBtn.UseVisualStyleBackColor = true;
            StatusBtn.Click += StatusBtn_Click;
            // 
            // MapBtn
            // 
            MapBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            MapBtn.Image = (Image)resources.GetObject("MapBtn.Image");
            MapBtn.Location = new Point(8, 72);
            MapBtn.Name = "MapBtn";
            MapBtn.Size = new Size(60, 60);
            MapBtn.TabIndex = 5;
            MapBtn.UseVisualStyleBackColor = true;
            MapBtn.Click += MapBtn_Click;
            // 
            // SurveyBtn
            // 
            SurveyBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SurveyBtn.Image = (Image)resources.GetObject("SurveyBtn.Image");
            SurveyBtn.Location = new Point(8, 6);
            SurveyBtn.Name = "SurveyBtn";
            SurveyBtn.Size = new Size(60, 60);
            SurveyBtn.TabIndex = 4;
            SurveyBtn.UseVisualStyleBackColor = true;
            SurveyBtn.Click += SurveyBtn_Click;
            // 
            // CalibrationBtn
            // 
            CalibrationBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            CalibrationBtn.Image = (Image)resources.GetObject("CalibrationBtn.Image");
            CalibrationBtn.Location = new Point(8, 542);
            CalibrationBtn.Name = "CalibrationBtn";
            CalibrationBtn.Size = new Size(60, 60);
            CalibrationBtn.TabIndex = 3;
            CalibrationBtn.UseVisualStyleBackColor = true;
            CalibrationBtn.Click += CalibrationBtn_Click;
            // 
            // EditSettingsBtn
            // 
            EditSettingsBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            EditSettingsBtn.Image = (Image)resources.GetObject("EditSettingsBtn.Image");
            EditSettingsBtn.Location = new Point(8, 608);
            EditSettingsBtn.Name = "EditSettingsBtn";
            EditSettingsBtn.Size = new Size(60, 60);
            EditSettingsBtn.TabIndex = 1;
            EditSettingsBtn.UseVisualStyleBackColor = true;
            EditSettingsBtn.Click += EditSettingsBtn_Click;
            // 
            // EditEquipmentBtn
            // 
            EditEquipmentBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            EditEquipmentBtn.Image = (Image)resources.GetObject("EditEquipmentBtn.Image");
            EditEquipmentBtn.Location = new Point(8, 674);
            EditEquipmentBtn.Name = "EditEquipmentBtn";
            EditEquipmentBtn.Size = new Size(60, 60);
            EditEquipmentBtn.TabIndex = 0;
            EditEquipmentBtn.UseVisualStyleBackColor = true;
            EditEquipmentBtn.Click += EditEquipmentBtn_Click;
            // 
            // ContentPanel
            // 
            ContentPanel.BackColor = SystemColors.Control;
            ContentPanel.Dock = DockStyle.Fill;
            ContentPanel.Location = new Point(76, 0);
            ContentPanel.Name = "ContentPanel";
            ContentPanel.Size = new Size(893, 714);
            ContentPanel.TabIndex = 4;
            // 
            // StatusPanel
            // 
            StatusPanel.BackColor = SystemColors.Control;
            StatusPanel.Controls.Add(StatusBar);
            StatusPanel.Dock = DockStyle.Bottom;
            StatusPanel.Location = new Point(76, 714);
            StatusPanel.Name = "StatusPanel";
            StatusPanel.Size = new Size(893, 26);
            StatusPanel.TabIndex = 0;
            // 
            // StatusBar
            // 
            StatusBar.Dock = DockStyle.Fill;
            StatusBar.Location = new Point(0, 0);
            StatusBar.Name = "StatusBar";
            StatusBar.ShowEStop = false;
            StatusBar.Size = new Size(893, 26);
            StatusBar.TabIndex = 0;
            // 
            // OpenFieldBtn
            // 
            OpenFieldBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            OpenFieldBtn.Image = (Image)resources.GetObject("OpenFieldBtn.Image");
            OpenFieldBtn.Location = new Point(8, 476);
            OpenFieldBtn.Name = "OpenFieldBtn";
            OpenFieldBtn.Size = new Size(60, 60);
            OpenFieldBtn.TabIndex = 7;
            OpenFieldBtn.UseVisualStyleBackColor = true;
            OpenFieldBtn.Click += OpenFieldBtn_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Green;
            ClientSize = new Size(1045, 740);
            Controls.Add(ContentPanel);
            Controls.Add(StatusPanel);
            Controls.Add(panel1);
            Controls.Add(panel3);
            DoubleBuffered = true;
            Name = "MainForm";
            Text = "AgGrade";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            panel3.ResumeLayout(false);
            panel1.ResumeLayout(false);
            StatusPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel panel3;
        private Panel panel1;
        private Panel ContentPanel;
        private Panel StatusPanel;
        private Button EditEquipmentBtn;
        private Button EditSettingsBtn;
        private Controls.IndicatorButton FrontBladeControlBtn;
        private Controls.IndicatorButton RearBladeControlBtn;
        private Button CalibrationBtn;
        private Button SurveyBtn;
        private Button MapBtn;
        private Button ZoomInBtn;
        private Button ZoomOutBtn;
        private Controls.StatusBar StatusBar;
        private Button StatusBtn;
        private Button ZoomFitBtn;
        private Panel FrontPanIndicator;
        private Panel RearPanIndicator;
        private Button OpenFieldBtn;
    }
}
