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
            ZoomInBtn = new Button();
            ZoomOutBtn = new Button();
            indicatorButton2 = new AgGrade.Controls.IndicatorButton();
            indicatorButton1 = new AgGrade.Controls.IndicatorButton();
            panel1 = new Panel();
            MapBtn = new Button();
            SurveyBtn = new Button();
            CalibrationBtn = new Button();
            EditSettingsBtn = new Button();
            EditEquipmentBtn = new Button();
            ContentPanel = new Panel();
            StatusPanel = new Panel();
            statusBar1 = new AgGrade.Controls.StatusBar();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            StatusPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panel3
            // 
            panel3.Controls.Add(ZoomInBtn);
            panel3.Controls.Add(ZoomOutBtn);
            panel3.Controls.Add(indicatorButton2);
            panel3.Controls.Add(indicatorButton1);
            panel3.Dock = DockStyle.Left;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(76, 495);
            panel3.TabIndex = 2;
            // 
            // ZoomInBtn
            // 
            ZoomInBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ZoomInBtn.Image = (Image)resources.GetObject("ZoomInBtn.Image");
            ZoomInBtn.Location = new Point(8, 363);
            ZoomInBtn.Name = "ZoomInBtn";
            ZoomInBtn.Size = new Size(60, 60);
            ZoomInBtn.TabIndex = 7;
            ZoomInBtn.UseVisualStyleBackColor = true;
            // 
            // ZoomOutBtn
            // 
            ZoomOutBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ZoomOutBtn.Image = (Image)resources.GetObject("ZoomOutBtn.Image");
            ZoomOutBtn.Location = new Point(8, 429);
            ZoomOutBtn.Name = "ZoomOutBtn";
            ZoomOutBtn.Size = new Size(60, 60);
            ZoomOutBtn.TabIndex = 6;
            ZoomOutBtn.UseVisualStyleBackColor = true;
            // 
            // indicatorButton2
            // 
            indicatorButton2.Image = (Image)resources.GetObject("indicatorButton2.Image");
            indicatorButton2.Indicator = AgGrade.Controls.IndicatorButton.IndicatorColor.Red;
            indicatorButton2.Location = new Point(8, 90);
            indicatorButton2.Name = "indicatorButton2";
            indicatorButton2.Size = new Size(60, 78);
            indicatorButton2.TabIndex = 1;
            // 
            // indicatorButton1
            // 
            indicatorButton1.Image = (Image)resources.GetObject("indicatorButton1.Image");
            indicatorButton1.Indicator = AgGrade.Controls.IndicatorButton.IndicatorColor.Red;
            indicatorButton1.Location = new Point(8, 6);
            indicatorButton1.Name = "indicatorButton1";
            indicatorButton1.Size = new Size(60, 78);
            indicatorButton1.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(MapBtn);
            panel1.Controls.Add(SurveyBtn);
            panel1.Controls.Add(CalibrationBtn);
            panel1.Controls.Add(EditSettingsBtn);
            panel1.Controls.Add(EditEquipmentBtn);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(669, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(76, 495);
            panel1.TabIndex = 3;
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
            CalibrationBtn.Location = new Point(8, 297);
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
            EditSettingsBtn.Location = new Point(8, 363);
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
            EditEquipmentBtn.Location = new Point(8, 429);
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
            ContentPanel.Size = new Size(593, 469);
            ContentPanel.TabIndex = 4;
            // 
            // StatusPanel
            // 
            StatusPanel.BackColor = SystemColors.Control;
            StatusPanel.Controls.Add(statusBar1);
            StatusPanel.Dock = DockStyle.Bottom;
            StatusPanel.Location = new Point(76, 469);
            StatusPanel.Name = "StatusPanel";
            StatusPanel.Size = new Size(593, 26);
            StatusPanel.TabIndex = 0;
            // 
            // statusBar1
            // 
            statusBar1.Dock = DockStyle.Fill;
            statusBar1.Location = new Point(0, 0);
            statusBar1.Name = "statusBar1";
            statusBar1.Size = new Size(593, 26);
            statusBar1.TabIndex = 0;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Green;
            ClientSize = new Size(745, 495);
            Controls.Add(ContentPanel);
            Controls.Add(StatusPanel);
            Controls.Add(panel1);
            Controls.Add(panel3);
            DoubleBuffered = true;
            Name = "MainForm";
            Text = "AgGrade";
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
        private Controls.IndicatorButton indicatorButton1;
        private Controls.IndicatorButton indicatorButton2;
        private Button CalibrationBtn;
        private Button SurveyBtn;
        private Button MapBtn;
        private Button ZoomInBtn;
        private Button ZoomOutBtn;
        private Controls.StatusBar statusBar1;
    }
}
