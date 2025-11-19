namespace AgGrade.Controls
{
    partial class EquipmentEditor
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
            VehicleBtn = new Button();
            FrontPanBtn = new Button();
            RearPanBtn = new Button();
            ContentPanel = new Panel();
            sectionTitle1 = new SectionTitle();
            SuspendLayout();
            // 
            // VehicleBtn
            // 
            VehicleBtn.BackColor = Color.Green;
            VehicleBtn.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            VehicleBtn.ForeColor = SystemColors.Control;
            VehicleBtn.Location = new Point(3, 54);
            VehicleBtn.Name = "VehicleBtn";
            VehicleBtn.Size = new Size(150, 45);
            VehicleBtn.TabIndex = 0;
            VehicleBtn.Text = "VEHICLE";
            VehicleBtn.UseVisualStyleBackColor = false;
            VehicleBtn.Click += SectionBtn_Click;
            // 
            // FrontPanBtn
            // 
            FrontPanBtn.BackColor = Color.Green;
            FrontPanBtn.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            FrontPanBtn.ForeColor = SystemColors.Control;
            FrontPanBtn.Location = new Point(159, 54);
            FrontPanBtn.Name = "FrontPanBtn";
            FrontPanBtn.Size = new Size(150, 45);
            FrontPanBtn.TabIndex = 1;
            FrontPanBtn.Text = "FRONT PAN";
            FrontPanBtn.UseVisualStyleBackColor = false;
            FrontPanBtn.Click += SectionBtn_Click;
            // 
            // RearPanBtn
            // 
            RearPanBtn.BackColor = Color.Green;
            RearPanBtn.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            RearPanBtn.ForeColor = SystemColors.Control;
            RearPanBtn.Location = new Point(315, 54);
            RearPanBtn.Name = "RearPanBtn";
            RearPanBtn.Size = new Size(150, 45);
            RearPanBtn.TabIndex = 2;
            RearPanBtn.Text = "REAR PAN";
            RearPanBtn.UseVisualStyleBackColor = false;
            RearPanBtn.Click += SectionBtn_Click;
            // 
            // ContentPanel
            // 
            ContentPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ContentPanel.Location = new Point(0, 105);
            ContentPanel.Name = "ContentPanel";
            ContentPanel.Size = new Size(794, 370);
            ContentPanel.TabIndex = 3;
            // 
            // sectionTitle1
            // 
            sectionTitle1.Dock = DockStyle.Top;
            sectionTitle1.Location = new Point(0, 0);
            sectionTitle1.Name = "sectionTitle1";
            sectionTitle1.Size = new Size(794, 48);
            sectionTitle1.TabIndex = 4;
            sectionTitle1.TitleText = "Equipment Settings";
            // 
            // EquipmentEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            Controls.Add(sectionTitle1);
            Controls.Add(ContentPanel);
            Controls.Add(RearPanBtn);
            Controls.Add(FrontPanBtn);
            Controls.Add(VehicleBtn);
            Name = "EquipmentEditor";
            Size = new Size(794, 475);
            Load += EquipmentEditor_Load;
            ResumeLayout(false);
        }

        #endregion

        private Button VehicleBtn;
        private Button FrontPanBtn;
        private Button RearPanBtn;
        private Panel ContentPanel;
        private SectionTitle sectionTitle1;
    }
}
