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
            indicatorButton1 = new AgGrade.Controls.IndicatorButton();
            panel1 = new Panel();
            EditSettingsBtn = new Button();
            EditEquipmentBtn = new Button();
            ContentPanel = new Panel();
            StatusPanel = new Panel();
            indicatorButton2 = new AgGrade.Controls.IndicatorButton();
            panel3.SuspendLayout();
            panel1.SuspendLayout();
            ContentPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panel3
            // 
            panel3.Controls.Add(indicatorButton2);
            panel3.Controls.Add(indicatorButton1);
            panel3.Dock = DockStyle.Left;
            panel3.Location = new Point(0, 0);
            panel3.Name = "panel3";
            panel3.Size = new Size(76, 495);
            panel3.TabIndex = 2;
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
            panel1.Controls.Add(EditSettingsBtn);
            panel1.Controls.Add(EditEquipmentBtn);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(669, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(76, 495);
            panel1.TabIndex = 3;
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
            ContentPanel.BackColor = SystemColors.ControlDark;
            ContentPanel.Controls.Add(StatusPanel);
            ContentPanel.Dock = DockStyle.Fill;
            ContentPanel.Location = new Point(76, 0);
            ContentPanel.Name = "ContentPanel";
            ContentPanel.Size = new Size(593, 495);
            ContentPanel.TabIndex = 4;
            // 
            // StatusPanel
            // 
            StatusPanel.Dock = DockStyle.Bottom;
            StatusPanel.Location = new Point(0, 463);
            StatusPanel.Name = "StatusPanel";
            StatusPanel.Size = new Size(593, 32);
            StatusPanel.TabIndex = 0;
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Green;
            ClientSize = new Size(745, 495);
            Controls.Add(ContentPanel);
            Controls.Add(panel1);
            Controls.Add(panel3);
            Name = "MainForm";
            Text = "AgGrade";
            panel3.ResumeLayout(false);
            panel1.ResumeLayout(false);
            ContentPanel.ResumeLayout(false);
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
    }
}
